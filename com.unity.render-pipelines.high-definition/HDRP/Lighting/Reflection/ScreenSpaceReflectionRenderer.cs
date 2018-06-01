using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class ScreenSpaceReflectionRenderer
    {
        public struct Settings
        {
            public int MaxRayAllocation;
        }

        struct CSMeta
        {
            static readonly Lit.ProjectionModel[] k_SupportedProjectionModels = { Lit.ProjectionModel.HiZ, Lit.ProjectionModel.Proxy };

            public static readonly int _SSReflectionRayHitNextTexture       = Shader.PropertyToID("_SSReflectionRayHitNextTexture");
            public static readonly int _SSReflectionRayHitNextSize          = Shader.PropertyToID("_SSReflectionRayHitNextSize");
            public static readonly int _SSReflectionRayHitNextScale         = Shader.PropertyToID("_SSReflectionRayHitNextScale");
            public static readonly int _Payload                             = Shader.PropertyToID("_Payload");
            public static readonly int _Payload1                            = Shader.PropertyToID("_Payload1");
            public static readonly int _Payload2                            = Shader.PropertyToID("_Payload2");
            public const int KAllocateRay_KernelSize = 8;
            public const int KCastRay_KernelSize = 8;

            Vector3Int[]         m_KCastRays_NumThreads;
            int[]                m_KCastRays;
            Vector3Int[]         m_KCastRaysDebug_NumThreads;
            int[]                m_KCastRaysDebug;
            public Vector3Int           KAllocateRays_NumThreads;
            public int                  KAllocateRays;
            public Vector3Int           KClear_NumThreads;
            public int                  KClear;

            public int GetKCastRays(
                Lit.ProjectionModel projectionModel, 
                bool debug
            )
            {
                return debug
                    ? m_KCastRaysDebug[(int)projectionModel]
                    : m_KCastRays[(int)projectionModel];
            }

            public Vector3Int GetKCastRays_NumThreads(
                Lit.ProjectionModel projectionModel, 
                bool debug
            )
            {
                return debug
                    ? m_KCastRaysDebug_NumThreads[(int)projectionModel]
                    : m_KCastRays_NumThreads[(int)projectionModel];
            }

            public void FindKernels(ComputeShader cs)
            {
                m_KCastRays_NumThreads = new Vector3Int[(int)Lit.ProjectionModel.Count];
                m_KCastRays = new int[(int)Lit.ProjectionModel.Count];
                m_KCastRaysDebug_NumThreads = new Vector3Int[(int)Lit.ProjectionModel.Count];
                m_KCastRaysDebug = new int[(int)Lit.ProjectionModel.Count];
                FindKernel(
                    cs,
                    "KAllocateRays_HiZ",
                    out KAllocateRays,
                    ref KAllocateRays_NumThreads
                );
                FindKernel(
                    cs,
                    "KClear",
                    out KClear,
                    ref KClear_NumThreads
                );

                for (int i = 0, c = k_SupportedProjectionModels.Length; i < c; ++i)
                {
                    FindKernel(
                        cs, 
                        "KCastRays_" + k_SupportedProjectionModels[i], 
                        out m_KCastRays[(int)k_SupportedProjectionModels[i]], 
                        ref m_KCastRays_NumThreads[(int)k_SupportedProjectionModels[i]]
                    );
                    FindKernel(
                        cs, 
                        "KCastRays_Debug_" + k_SupportedProjectionModels[i], 
                        out m_KCastRaysDebug[(int)k_SupportedProjectionModels[i]],
                        ref m_KCastRaysDebug_NumThreads[(int)k_SupportedProjectionModels[i]]
                    );
                }
            }

            void FindKernel(ComputeShader cs, string name, out int id, ref Vector3Int threads)
            {
                uint x, y, z;
                id = cs.FindKernel(name);
                cs.GetKernelThreadGroupSizes(id, out x, out y, out z);
                threads.Set((int)x, (int)y, (int)z);
            }
        }

        ComputeShader m_CS;
        CSMeta m_Kernels;
        RTHandleSystem m_RTHSystem;
        ComputeBuffer m_PayloadBuffer;
        ComputeBuffer m_Payload1Buffer;
        ComputeBuffer m_Payload2Buffer;

        Settings m_Settings;

        public ScreenSpaceReflectionRenderer(
            Settings settings,
            RTHandleSystem rthSystem,
            ComputeShader cs
        )
        {
            m_RTHSystem = rthSystem;
            m_CS = cs;
            m_Kernels.FindKernels(m_CS);
            m_Settings = settings;
        }

        public void AllocateBuffers()
        {
            m_PayloadBuffer = new ComputeBuffer(
                m_Settings.MaxRayAllocation + 3, 
                Marshal.SizeOf(typeof(uint)), 
                ComputeBufferType.IndirectArguments
            );
            m_Payload1Buffer = new ComputeBuffer(
                m_Settings.MaxRayAllocation, 
                Marshal.SizeOf(typeof(float)) * 4, 
                ComputeBufferType.Default
            );
            m_Payload2Buffer = new ComputeBuffer(
                m_Settings.MaxRayAllocation, 
                Marshal.SizeOf(typeof(float)) * 4, 
                ComputeBufferType.Default
            );
        }

        public void ClearBuffers(CommandBuffer cmd, HDCamera hdCamera)
        {
            RenderPassClear(hdCamera, cmd);
        }

        public void ReleaseBuffers()
        {
            m_PayloadBuffer.Release();
            m_PayloadBuffer = null;
            m_Payload1Buffer.Release();
            m_Payload1Buffer = null;
            m_Payload2Buffer.Release();
            m_Payload2Buffer = null;
        }

        public void PushGlobalParams(HDCamera hdCamera, CommandBuffer cmd, FrameSettings frameSettings)
        {
            Assert.IsNotNull(hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.SSReflectionRayHit));

            cmd.SetGlobalRTHandle(
                hdCamera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.SSReflectionRayHit),
                HDShaderIDs._SSReflectionRayHitTexture,
                HDShaderIDs._SSReflectionRayHitSize,
                HDShaderIDs._SSReflectionRayHitScale
            );
        }

        public void RenderPassCastRays(
            HDCamera hdCamera, 
            CommandBuffer cmd, 
            bool debug,
            RTHandleSystem.RTHandle debugTextureHandle
        )
        {
            var ssReflection = VolumeManager.instance.stack.GetComponent<ScreenSpaceReflection>()
                    ?? ScreenSpaceReflection.@default;

            var projectionModel     = (Lit.ProjectionModel)ssReflection.deferredProjectionModel.value;

            if (projectionModel == Lit.ProjectionModel.HiZ)
                RenderPassAllocateRays(hdCamera, cmd);

            var kernel              = m_Kernels.GetKCastRays(projectionModel, debug);
            var threadGroups        = new Vector3Int(
                                        // We use 8x8 kernel for KCastRays
                                        Mathf.CeilToInt((hdCamera.actualWidth) / (float)CSMeta.KCastRay_KernelSize),
                                        Mathf.CeilToInt((hdCamera.actualHeight) / (float)CSMeta.KCastRay_KernelSize),
                                        1
                                    );

            using (new ProfilingSample(cmd, "Screen Space Reflection - Cast Rays", CustomSamplerId.SSRCastRays.GetSampler()))
            {
                var currentRTHRayHit = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.SSReflectionRayHit);
                Assert.IsNotNull(currentRTHRayHit);

                if (debug)
                {
                    cmd.SetComputeTextureParam(
                        m_CS,
                        kernel,
                        HDShaderIDs._DebugTexture,
                        debugTextureHandle
                    );
                }

                if (projectionModel == Lit.ProjectionModel.HiZ)
                {
                    cmd.SetComputeBufferParam(
                        m_CS,
                        kernel,
                        CSMeta._Payload,
                        m_PayloadBuffer
                    );
                    cmd.SetComputeBufferParam(
                        m_CS,
                        kernel,
                        CSMeta._Payload1,
                        m_Payload1Buffer
                    );
                    cmd.SetComputeBufferParam(
                        m_CS,
                        kernel,
                        CSMeta._Payload2,
                        m_Payload2Buffer
                    );
                }
                
                cmd.SetComputeRTHandleParam(
                    m_CS,
                    kernel,
                    currentRTHRayHit,
                    CSMeta._SSReflectionRayHitNextTexture,
                    CSMeta._SSReflectionRayHitNextSize,
                    CSMeta._SSReflectionRayHitNextScale
                );
                if (projectionModel == Lit.ProjectionModel.HiZ)
                {
                    cmd.DispatchCompute(
                        m_CS,
                        kernel,
                        m_PayloadBuffer,
                        0
                    );
                }
                else
                {
                    cmd.DispatchCompute(
                        m_CS,
                        kernel,
                        threadGroups.x, threadGroups.y, threadGroups.z
                    );
                }
                
                cmd.SetGlobalRTHandle(
                    currentRTHRayHit,
                    HDShaderIDs._SSReflectionRayHitTexture,
                    HDShaderIDs._SSReflectionRayHitSize,
                    HDShaderIDs._SSReflectionRayHitScale
                );
            }
        }

        void RenderPassAllocateRays(
            HDCamera hdCamera, 
            CommandBuffer cmd
        )
        {
            var kernel              = m_Kernels.KAllocateRays;
            var threadGroups        = new Vector3Int(
                                        Mathf.CeilToInt((hdCamera.actualWidth) / (float)CSMeta.KAllocateRay_KernelSize),
                                        Mathf.CeilToInt((hdCamera.actualHeight) / (float)CSMeta.KAllocateRay_KernelSize),
                                        1
                                    );

            using (new ProfilingSample(cmd, "Screen Space Reflection - Allocate Rays", CustomSamplerId.SSRAllocateRays.GetSampler()))
            {
                cmd.SetComputeBufferParam(
                    m_CS,
                    kernel,
                    CSMeta._Payload,
                    m_PayloadBuffer
                );
                cmd.SetComputeBufferParam(
                    m_CS,
                    kernel,
                    CSMeta._Payload1,
                    m_Payload1Buffer
                );
                cmd.SetComputeBufferParam(
                    m_CS,
                    kernel,
                    CSMeta._Payload2,
                    m_Payload2Buffer
                );
                cmd.DispatchCompute(
                    m_CS,
                    kernel,
                    threadGroups.x, threadGroups.y, threadGroups.z
                );
            }
        }

         void RenderPassClear(
            HDCamera hdCamera, 
            CommandBuffer cmd
        )
        {
            var kernel              = m_Kernels.KClear;
            var threadGroups        = new Vector3Int(1, 1, 1);

            using (new ProfilingSample(cmd, "Screen Space Reflection - Clear Buffers", CustomSamplerId.SSRClear.GetSampler()))
            {
                cmd.SetComputeBufferParam(
                    m_CS,
                    kernel,
                    CSMeta._Payload,
                    m_PayloadBuffer
                );
                cmd.DispatchCompute(
                    m_CS,
                    kernel,
                    threadGroups.x, threadGroups.y, threadGroups.z
                );
            }
        }

        public void AllocateCameraBuffersIfRequired(HDCamera hdCamera)
        {
            if (hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.SSReflectionRayHit) == null)
                hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.SSReflectionRayHit, AllocateCameraBufferRayHit);
        }

        RTHandleSystem.RTHandle AllocateCameraBufferRayHit(string id, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(
                Vector2.one,
                filterMode: FilterMode.Point,
                colorFormat: RenderTextureFormat.ARGBInt,
                sRGB: false,
                useMipMap: false,
                autoGenerateMips: false,
                enableRandomWrite: true,
                name: string.Format("SSRRayHit-{0}-{1}", id, frameIndex)
            );
        }
    }
}