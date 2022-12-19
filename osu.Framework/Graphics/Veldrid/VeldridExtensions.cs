// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Logging;
using osuTK.Graphics;
using SharpGen.Runtime;
using Veldrid;
using Veldrid.MetalBindings;
using Veldrid.OpenGLBinding;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vulkan;
using GetPName = Veldrid.OpenGLBinding.GetPName;
using GraphicsBackend = Veldrid.GraphicsBackend;
using PrimitiveTopology = Veldrid.PrimitiveTopology;
using StencilOperation = Veldrid.StencilOperation;
using StringName = Veldrid.OpenGLBinding.StringName;
using VertexAttribPointerType = osuTK.Graphics.ES30.VertexAttribPointerType;

namespace osu.Framework.Graphics.Veldrid
{
    internal static class VeldridExtensions
    {
        public static RgbaFloat ToRgbaFloat(this Color4 colour) => new RgbaFloat(colour.R, colour.G, colour.B, colour.A);

        public static BlendFactor ToBlendFactor(this BlendingType type)
        {
            switch (type)
            {
                case BlendingType.DstAlpha:
                    return BlendFactor.DestinationAlpha;

                case BlendingType.DstColor:
                    return BlendFactor.DestinationColor;

                case BlendingType.SrcAlpha:
                    return BlendFactor.SourceAlpha;

                case BlendingType.SrcColor:
                    return BlendFactor.SourceColor;

                case BlendingType.OneMinusDstAlpha:
                    return BlendFactor.InverseDestinationAlpha;

                case BlendingType.OneMinusDstColor:
                    return BlendFactor.InverseDestinationColor;

                case BlendingType.OneMinusSrcAlpha:
                    return BlendFactor.InverseSourceAlpha;

                case BlendingType.OneMinusSrcColor:
                    return BlendFactor.InverseSourceColor;

                case BlendingType.One:
                    return BlendFactor.One;

                case BlendingType.Zero:
                    return BlendFactor.Zero;

                case BlendingType.ConstantColor:
                    return BlendFactor.BlendFactor;

                case BlendingType.OneMinusConstantColor:
                    return BlendFactor.InverseBlendFactor;

                // todo: veldrid has no support for those, we may want to consider removing them from BlendingType enum (we don't even provide a blend factor in the parameters).
                case BlendingType.ConstantAlpha:
                case BlendingType.OneMinusConstantAlpha:
                case BlendingType.SrcAlphaSaturate:
                default:
                    return default;
            }
        }

        public static BlendFunction ToBlendFunction(this BlendingEquation equation)
        {
            switch (equation)
            {
                case BlendingEquation.Add:
                    return BlendFunction.Add;

                case BlendingEquation.Subtract:
                    return BlendFunction.Subtract;

                case BlendingEquation.ReverseSubtract:
                    return BlendFunction.ReverseSubtract;

                case BlendingEquation.Min:
                    return BlendFunction.Minimum;

                case BlendingEquation.Max:
                    return BlendFunction.Maximum;

                default:
                    throw new ArgumentOutOfRangeException(nameof(equation));
            }
        }

        public static ColorWriteMask ToColorWriteMask(this BlendingMask mask)
        {
            ColorWriteMask writeMask = ColorWriteMask.None;

            if (mask.HasFlagFast(BlendingMask.Red)) writeMask |= ColorWriteMask.Red;
            if (mask.HasFlagFast(BlendingMask.Green)) writeMask |= ColorWriteMask.Green;
            if (mask.HasFlagFast(BlendingMask.Blue)) writeMask |= ColorWriteMask.Blue;
            if (mask.HasFlagFast(BlendingMask.Alpha)) writeMask |= ColorWriteMask.Alpha;

            return writeMask;
        }

        public static ComparisonKind ToComparisonKind(this BufferTestFunction function)
        {
            switch (function)
            {
                case BufferTestFunction.Always:
                    return ComparisonKind.Always;

                case BufferTestFunction.Never:
                    return ComparisonKind.Never;

                case BufferTestFunction.LessThan:
                    return ComparisonKind.Less;

                case BufferTestFunction.Equal:
                    return ComparisonKind.Equal;

                case BufferTestFunction.LessThanOrEqual:
                    return ComparisonKind.LessEqual;

                case BufferTestFunction.GreaterThan:
                    return ComparisonKind.Greater;

                case BufferTestFunction.NotEqual:
                    return ComparisonKind.NotEqual;

                case BufferTestFunction.GreaterThanOrEqual:
                    return ComparisonKind.GreaterEqual;

                default:
                    throw new ArgumentOutOfRangeException(nameof(function));
            }
        }

        public static StencilOperation ToStencilOperation(this Rendering.StencilOperation operation)
        {
            switch (operation)
            {
                case Rendering.StencilOperation.Zero:
                    return StencilOperation.Zero;

                case Rendering.StencilOperation.Invert:
                    return StencilOperation.Invert;

                case Rendering.StencilOperation.Keep:
                    return StencilOperation.Keep;

                case Rendering.StencilOperation.Replace:
                    return StencilOperation.Replace;

                case Rendering.StencilOperation.Increase:
                    return StencilOperation.IncrementAndClamp;

                case Rendering.StencilOperation.Decrease:
                    return StencilOperation.DecrementAndClamp;

                case Rendering.StencilOperation.IncreaseWrap:
                    return StencilOperation.IncrementAndWrap;

                case Rendering.StencilOperation.DecreaseWrap:
                    return StencilOperation.DecrementAndWrap;

                default:
                    throw new ArgumentOutOfRangeException(nameof(operation));
            }
        }

        public static VertexElementFormat ToVertexElementFormat(this VertexAttribPointerType type, int count)
        {
            switch (type)
            {
                case VertexAttribPointerType.Byte when count == 2:
                    return VertexElementFormat.SByte2;

                case VertexAttribPointerType.Byte when count == 4:
                    return VertexElementFormat.SByte4;

                case VertexAttribPointerType.UnsignedByte when count == 2:
                    return VertexElementFormat.Byte2;

                case VertexAttribPointerType.UnsignedByte when count == 4:
                    return VertexElementFormat.Byte4;

                case VertexAttribPointerType.Short when count == 2:
                    return VertexElementFormat.Short2;

                case VertexAttribPointerType.Short when count == 4:
                    return VertexElementFormat.Short4;

                case VertexAttribPointerType.UnsignedShort when count == 2:
                    return VertexElementFormat.UShort2;

                case VertexAttribPointerType.UnsignedShort when count == 4:
                    return VertexElementFormat.UShort4;

                case VertexAttribPointerType.Int when count == 1:
                    return VertexElementFormat.Int1;

                case VertexAttribPointerType.Int when count == 2:
                    return VertexElementFormat.Int2;

                case VertexAttribPointerType.Int when count == 3:
                    return VertexElementFormat.Int3;

                case VertexAttribPointerType.Int when count == 4:
                    return VertexElementFormat.Int4;

                case VertexAttribPointerType.UnsignedInt when count == 1:
                    return VertexElementFormat.UInt1;

                case VertexAttribPointerType.UnsignedInt when count == 2:
                    return VertexElementFormat.UInt2;

                case VertexAttribPointerType.UnsignedInt when count == 3:
                    return VertexElementFormat.UInt3;

                case VertexAttribPointerType.UnsignedInt when count == 4:
                    return VertexElementFormat.UInt4;

                case VertexAttribPointerType.Float when count == 1:
                    return VertexElementFormat.Float1;

                case VertexAttribPointerType.Float when count == 2:
                    return VertexElementFormat.Float2;

                case VertexAttribPointerType.Float when count == 3:
                    return VertexElementFormat.Float3;

                case VertexAttribPointerType.Float when count == 4:
                    return VertexElementFormat.Float4;

                case VertexAttribPointerType.HalfFloat when count == 1:
                    return VertexElementFormat.Half1;

                case VertexAttribPointerType.HalfFloat when count == 2:
                    return VertexElementFormat.Half2;

                case VertexAttribPointerType.HalfFloat when count == 4:
                    return VertexElementFormat.Half4;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public static PrimitiveTopology ToPrimitiveTopology(this Rendering.PrimitiveTopology type)
        {
            switch (type)
            {
                case Rendering.PrimitiveTopology.Points:
                    return PrimitiveTopology.PointList;

                case Rendering.PrimitiveTopology.Lines:
                    return PrimitiveTopology.LineList;

                case Rendering.PrimitiveTopology.LineStrip:
                    return PrimitiveTopology.LineStrip;

                case Rendering.PrimitiveTopology.Triangles:
                    return PrimitiveTopology.TriangleList;

                case Rendering.PrimitiveTopology.TriangleStrip:
                    return PrimitiveTopology.TriangleStrip;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        public static void LogD3D11(this GraphicsDevice device, out int maxTextureSize)
        {
            Debug.Assert(device.BackendType == GraphicsBackend.Direct3D11);

            var info = device.GetD3D11Info();
            var dxgiAdapter = MarshallingHelpers.FromPointer<IDXGIAdapter>(info.Adapter);
            var d3d11Device = MarshallingHelpers.FromPointer<ID3D11Device>(info.Device);

            maxTextureSize = ID3D11Resource.MaximumTexture2DSize;

            Logger.Log($@"Direct3D 11 Initialized
                        Direct3D 11 Feature Level:           {d3d11Device.FeatureLevel.ToString().Replace("Level_", string.Empty).Replace("_", ".")}
                        Direct3D 11 Adapter:                 {dxgiAdapter.Description.Description}
                        Direct3D 11 Dedicated Video Memory:  {dxgiAdapter.Description.DedicatedVideoMemory / 1024 / 1024} MB
                        Direct3D 11 Dedicated System Memory: {dxgiAdapter.Description.DedicatedSystemMemory / 1024 / 1024} MB
                        Direct3D 11 Shared System Memory:    {dxgiAdapter.Description.SharedSystemMemory / 1024 / 1024} MB");
        }

        public static unsafe void LogOpenGL(this GraphicsDevice device, out int maxTextureSize)
        {
            var info = device.GetOpenGLInfo();

            string version = string.Empty;
            string renderer = string.Empty;
            string glslVersion = string.Empty;
            string vendor = string.Empty;
            string extensions = string.Empty;

            int glMaxTextureSize = 0;

            info.ExecuteOnGLThread(() =>
            {
                version = Marshal.PtrToStringUTF8((IntPtr)OpenGLNative.glGetString(StringName.Version)) ?? string.Empty;
                renderer = Marshal.PtrToStringUTF8((IntPtr)OpenGLNative.glGetString(StringName.Renderer)) ?? string.Empty;
                vendor = Marshal.PtrToStringUTF8((IntPtr)OpenGLNative.glGetString(StringName.Vendor)) ?? string.Empty;
                glslVersion = Marshal.PtrToStringUTF8((IntPtr)OpenGLNative.glGetString(StringName.ShadingLanguageVersion)) ?? string.Empty;
                extensions = string.Join(' ', info.Extensions);

                int size;
                OpenGLNative.glGetIntegerv(GetPName.MaxTextureSize, &size);
                glMaxTextureSize = size;
            });

            maxTextureSize = glMaxTextureSize;

            Logger.Log($@"GL Initialized
                                    GL Version:                 {version}
                                    GL Renderer:                {renderer}
                                    GL Shader Language version: {glslVersion}
                                    GL Vendor:                  {vendor}
                                    GL Extensions:              {extensions}");
        }

        public static unsafe void LogVulkan(this GraphicsDevice device, out int maxTextureSize)
        {
            Debug.Assert(device.BackendType == GraphicsBackend.Vulkan);

            var info = device.GetVulkanInfo();
            var physicalDevice = info.PhysicalDevice;

            uint instanceExtensionsCount = 0;
            var result = VulkanNative.vkEnumerateInstanceExtensionProperties((byte*)null, ref instanceExtensionsCount, IntPtr.Zero);

            var instanceExtensions = new VkExtensionProperties[(int)instanceExtensionsCount];
            if (result == VkResult.Success && instanceExtensionsCount > 0)
                VulkanNative.vkEnumerateInstanceExtensionProperties((byte*)null, ref instanceExtensionsCount, ref instanceExtensions[0]);

            uint deviceExetnsionsCount = 0;
            result = VulkanNative.vkEnumerateDeviceExtensionProperties(physicalDevice, (byte*)null, ref deviceExetnsionsCount, IntPtr.Zero);

            var deviceExtensions = new VkExtensionProperties[(int)deviceExetnsionsCount];
            if (result == VkResult.Success && deviceExetnsionsCount > 0)
                VulkanNative.vkEnumerateDeviceExtensionProperties(physicalDevice, (byte*)null, ref deviceExetnsionsCount, ref deviceExtensions[0]);

            VkPhysicalDeviceProperties properties;
            VulkanNative.vkGetPhysicalDeviceProperties(physicalDevice, &properties);

            maxTextureSize = (int)properties.limits.maxImageDimension2D;

            string vulkanName = RuntimeInfo.IsApple ? "MoltenVK" : "Vulkan";
            string extensions = string.Join(" ", instanceExtensions.Concat(deviceExtensions).Select(e => Marshal.PtrToStringUTF8((IntPtr)e.extensionName)));

            string apiVersion = $"{properties.apiVersion >> 22}.{(properties.apiVersion >> 12) & 0x3FFU}.{properties.apiVersion & 0xFFFU}";
            string driverVersion;

            // https://github.com/SaschaWillems/vulkan.gpuinfo.org/blob/1e6ca6e3c0763daabd6a101b860ab4354a07f5d3/functions.php#L293-L325
            if (properties.vendorID == 0x10DE) // NVIDIA's versioning convention
                driverVersion = $"{properties.driverVersion >> 22}.{(properties.driverVersion >> 14) & 0x0FFU}.{(properties.driverVersion >> 6) & 0x0FFU}.{properties.driverVersion & 0x003U}";
            else if (properties.vendorID == 0x8086 && RuntimeInfo.OS == RuntimeInfo.Platform.Windows) // Intel's versioning convention on Windows
                driverVersion = $"{properties.driverVersion >> 22}.{properties.driverVersion & 0x3FFFU}";
            else // Vulkan's convention
                driverVersion = $"{properties.driverVersion >> 22}.{(properties.driverVersion >> 12) & 0x3FFU}.{properties.driverVersion & 0xFFFU}";

            Logger.Log($@"{vulkanName} Initialized
                                    {vulkanName} API Version:    {apiVersion}
                                    {vulkanName} Driver Version: {driverVersion}
                                    {vulkanName} Device:         {Marshal.PtrToStringUTF8((IntPtr)properties.deviceName)}
                                    {vulkanName} Extensions:     {extensions}");
        }

        public static void LogMetal(this GraphicsDevice device, out int maxTextureSize)
        {
            Debug.Assert(device.BackendType == GraphicsBackend.Metal);

            var info = device.GetMetalInfo();

            string[] featureSetParts = info.MaxFeatureSet.ToString().Split('_');
            string featureDevice = featureSetParts[0];
            string featureFamily = featureSetParts[1].Replace("GPUFamily", string.Empty);
            string featureVersion = featureSetParts[2];

            // https://developer.apple.com/metal/Metal-Feature-Set-Tables.pdf
            if (info.MaxFeatureSet <= MTLFeatureSet.iOS_GPUFamily4_v1)
                maxTextureSize = info.MaxFeatureSet <= MTLFeatureSet.iOS_GPUFamily1_v4 ? 8192 : 16384;
            else if (info.MaxFeatureSet <= MTLFeatureSet.tvOS_GPUFamily2_v1)
                maxTextureSize = info.MaxFeatureSet <= MTLFeatureSet.tvOS_GPUFamily1_v3 ? 8192 : 16384;
            else
                maxTextureSize = 16384;

            Logger.Log($@"Metal Initialized
                        Metal Feature Set: {featureDevice} GPU family {featureFamily} ({featureVersion})");
        }
    }
}
