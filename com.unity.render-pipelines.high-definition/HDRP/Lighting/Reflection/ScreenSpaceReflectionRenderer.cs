using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class ScreenSpaceReflectionRenderer
    {
        struct CSMeta
        {
            static readonly Lit.ProjectionModel[] k_SupportedProjectionModels = { Lit.ProjectionModel.HiZ, Lit.ProjectionModel.Proxy };

            public static readonly int _SSReflectionRayHitNextTexture       = Shader.PropertyToID("_SSReflectionRayHitNextTexture");
            public static readonly int _SSReflectionRayHitNextSize          = Shader.PropertyToID("_SSReflectionRayHitNextSize");
            public static readonly int _SSReflectionRayHitNextScale         = Shader.PropertyToID("_SSReflectionRayHitNextScale");
            public const int KCastRay_KernelSize = 8;

            Vector3Int[]         m_KCastRays_NumThreads;
            int[]                m_KCastRays;
            Vector3Int[]         m_KCastRaysDebug_NumThreads;
            int[]                m_KCastRaysDebug;

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

        public ScreenSpaceReflectionRenderer(
            RTHandleSystem rthSystem,
            ComputeShader cs
        )
        {
            m_RTHSystem = rthSystem;
            m_CS = cs;
            m_Kernels.FindKernels(m_CS);
        }

        public void AllocateBuffers()
        {
        }

        public void ClearBuffers()
        {
        }

        public void ReleaseBuffers()
        {
            
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

        public void RenderPassCastRays(HDCamera hdCamera, CommandBuffer cmd, bool debug)
        {
            var ssReflection = VolumeManager.instance.stack.GetComponent<ScreenSpaceReflection>()
                    ?? ScreenSpaceReflection.@default;

            var projectionModel     = (Lit.ProjectionModel)ssReflection.deferredProjectionModel.value;
            var kernel              = m_Kernels.GetKCastRays(projectionModel, debug);
            var threadGroups        = new Vector3Int(
                                        // We use 8x8 kernel for KCastRays
                                        (int)((hdCamera.actualWidth + CSMeta.KCastRay_KernelSize - 1) / (float)CSMeta.KCastRay_KernelSize),
                                        (int)((hdCamera.actualHeight + CSMeta.KCastRay_KernelSize - 1) / (float)CSMeta.KCastRay_KernelSize),
                                        1
                                    );

            using (new ProfilingSample(cmd, "Screen Space Reflection - Cast Rays", CustomSamplerId.SSRCastRays.GetSampler()))
            {
                var currentRTHRayHit = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.SSReflectionRayHit);
                Assert.IsNotNull(currentRTHRayHit);

                cmd.SetComputeRTHandleParam(
                    m_CS,
                    kernel,
                    currentRTHRayHit,
                    CSMeta._SSReflectionRayHitNextTexture,
                    CSMeta._SSReflectionRayHitNextSize,
                    CSMeta._SSReflectionRayHitNextScale
                );
                cmd.DispatchCompute(
                    m_CS,
                    kernel,
                    threadGroups.x, threadGroups.y, threadGroups.z
                );
                cmd.SetGlobalRTHandle(
                    currentRTHRayHit,
                    HDShaderIDs._SSReflectionRayHitTexture,
                    HDShaderIDs._SSReflectionRayHitSize,
                    HDShaderIDs._SSReflectionRayHitScale
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