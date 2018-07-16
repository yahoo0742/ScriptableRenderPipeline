using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Vector", "Rotate About Axis")]
    public class RotateAboutAxisNode : CodeFunctionNode
    {
        public RotateAboutAxisNode()
        {
            name = "Rotate About Axis";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Rotate-About-Axis-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Rotate_About_Axis", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Rotate_About_Axis(
            [Slot(0, Binding.None)] Vector3 In,
            [Slot(1, Binding.None)] Vector3 Axis,
            [Slot(2, Binding.None)] Vector1 Degrees,
            [Slot(3, Binding.None)] out Vector3 Out)
        {
            Out = In;
            return
                @"
{
    {precision} s = sin(Degrees);
    {precision} c = cos(Degrees);
    {precision} one_minus_c = 1.0 - c;
    
    Axis = normalize(Axis);

    {precision}3x3 rot_mat = { one_minus_c * Axis.x * Axis.x + c,            one_minus_c * Axis.x * Axis.y - Axis.z * s,     one_minus_c * Axis.z * Axis.x + Axis.y * s,
                               one_minus_c * Axis.x * Axis.y + Axis.z * s,   one_minus_c * Axis.y * Axis.y + c,              one_minus_c * Axis.y * Axis.z - Axis.x * s,
                               one_minus_c * Axis.z * Axis.x - Axis.y * s,   one_minus_c * Axis.y * Axis.z + Axis.x * s,     one_minus_c * Axis.z * Axis.z + c
                             };

    Out = mul(rot_mat,  In);
}
";
        }
    }
}
