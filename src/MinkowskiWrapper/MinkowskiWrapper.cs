using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Minkowski
{
    public class MinkowskiWrapper
    {
        //Copied from chatgpt to handle loading dll
        static MinkowskiWrapper()
        {
            LoadNativeDll();
        }

        private static void LoadNativeDll()
        {
            try
            {
                int[] sizes = new int[1];
                getSizes1(sizes);
            }
            catch (DllNotFoundException)
            {
                try
                {
                    var assemblyLocation = Assembly.GetExecutingAssembly().Location;
                    var assemblyDir = Path.GetDirectoryName(assemblyLocation);
                    var dllPath = Path.Combine(assemblyDir, "minkowski.dll");

                    if (File.Exists(dllPath))
                    {
                        LoadLibrary(dllPath);
                    }
                    else
                    {
                        throw new DllNotFoundException(
                            "minkowski.dll was not found in the expected locations. " +
                            "Ensure it's included in the NuGet package and copied to the output directory.");
                    }
                }
                catch (Exception ex)
                {
                    throw new DllNotFoundException(
                        $"Failed to load minkowski.dll: {ex.Message}", ex);
                }
            }
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("minkowski.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void setData(int cntA, double[] pntsA, int holesCnt, int[] holesSizes, double[] holesPoints, int cntB, double[] pntsB);

        [DllImport("minkowski.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void calculateNFP();

        [DllImport("minkowski.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void getSizes1(int[] sizes);

        [DllImport("minkowski.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void getSizes2(int[] sizes1, int[] sizes2);

        [DllImport("minkowski.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void getResults(double[] data, double[] holesData);
    }
}