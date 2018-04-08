using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrashNSaneLoadDetector
{
	//This class contains settings, features and methods for computing features from a given Bitmap
	class FeatureDetector
	{
		//used as a cutoff for when a match is detected correctly
		//private static float varianceOfBinsAllowed = 1.0f;
		private static float varianceOfBinsAllowedMult = 1.45f;
		private static float additiveVariance = 2.0f;
		private static int patchSizeX = 50;
		private static int patchSizeY = 50;
		public static int numberOfBinsCorrect = 450;
		private static int numberOfBins = 16;

		public static bool compareFeatureVector(int[] newVector, out int matchingBins, bool debugOutput = true)
		{
			int[,] comparison_vectors = listOfFeatureVectorsEng;
			int size = newVector.Length;

			if (comparison_vectors.GetLength(1) < size)
			{
				size = comparison_vectors.GetLength(1);
			}

			//int number_of_bins_needed = 290;// (int) (size * percent_of_bins_correct);
			
			int numVectors = comparison_vectors.GetLength(0);


			matchingBins = 0;
			for (int vectorIndex = 0; vectorIndex < numVectors; vectorIndex++)
			{
				int tempMatchingBins = 0;
				//check if the current feature vector matches one of the stored ones closely enough
				for (int bin = 0; bin < size; bin++)
				{
					//Determine upper/lower histogram ranges for matching bins
					int lower_bound = (int)((comparison_vectors[vectorIndex, bin] / varianceOfBinsAllowedMult) - additiveVariance);
					int upper_bound = (int)((comparison_vectors[vectorIndex, bin] * varianceOfBinsAllowedMult) + additiveVariance);

					
					
					if (newVector[bin] <= upper_bound && newVector[bin] >= lower_bound)
					{
						tempMatchingBins++;
					}

					//If we can not get a possible match anymore, break for speed
					if ((bin - tempMatchingBins) > (size - numberOfBinsCorrect))
					{
						break;
					}
				}
				matchingBins = Math.Max(matchingBins, tempMatchingBins);
				

				
			}


			

			if(debugOutput)
			{
				System.Console.WriteLine("Matching bins: " + matchingBins);
			}

			if (matchingBins >= numberOfBinsCorrect)
			{
				//if we found enough similarities, we found a match.
				return true;
			}

			return false;
		}

		public static List<int> featuresFromBitmap(Bitmap capture)
		{
			List<int> features = new List<int>();

			BitmapData bData = capture.LockBits(new Rectangle(0, 0, capture.Width, capture.Height), ImageLockMode.ReadWrite, capture.PixelFormat);
			int bmpStride = bData.Stride;
			int size = bData.Stride * bData.Height;

			byte[] data = new byte[size];

			/*This overload copies data of /size/ into /data/ from location specified (/Scan0/)*/
			System.Runtime.InteropServices.Marshal.Copy(bData.Scan0, data, 0, size);
			int yAdd = 0;
			int r = 0;
			int g = 0;
			int b = 0;
			//we look at 50x50 patches and compute histogram bins for the a/r/g/b values.

			int stride = 1; //spacing between feature pixels

			for (int patchX = 0; patchX < (capture.Width / patchSizeX); patchX++)
			{
				for (int patchY = 0; patchY < (capture.Height / patchSizeY); patchY++)
				{
					//int[] patch_hist_a = new int[numberOfBins];
					int[] patchHistR = new int[numberOfBins];
					int[] patchHistG = new int[numberOfBins];
					int[] patchHistB = new int[numberOfBins];

					int xStart = patchX * (patchSizeX * stride);
					int yStart = patchY * (patchSizeX * stride);
					int xEnd = (patchX + 1) * (patchSizeX * stride);
					int yEnd = (patchY + 1) * (patchSizeY * stride);

					for (int x_index = xStart; x_index < xEnd; x_index += stride)
					{
						for (int y_index = yStart; y_index < yEnd; y_index += stride)
						{
							yAdd = y_index * bmpStride;

							//NOTE: while the pixel format is 32ARGB, reading byte-wise results in BGRA.
							b = (int)(data[(x_index * 4) + (yAdd) + 0]);
							g = (int)(data[(x_index * 4) + (yAdd) + 1]);
							r = (int)(data[(x_index * 4) + (yAdd) + 2]);
							
							patchHistR[(r * numberOfBins) / 256]++;
							patchHistG[(g * numberOfBins) / 256]++;
							patchHistB[(b * numberOfBins) / 256]++;
							
							
						}
					}

					//enter the histograms as our features
					features.AddRange(patchHistR);
					features.AddRange(patchHistG);
					features.AddRange(patchHistB);
				}
			}

			capture.UnlockBits(bData);

			return features;
		}








		//this list of vectors is for 300x100, patchSize = 50, numberOfBins = 16
		//to adapt - if wrongly detected pause, increase match threshold and add wrongly detected runs to list
		//btw - I know this is ugly, but I want the .asl script to be self-contained, otherwise I'd have stored in in .csv or .json or whatever
		private static int[,] listOfFeatureVectorsEng = {
//{1140,189,88,46,57,54,55,60,42,40,55,62,47,53,69,443,1365,90,88,65,55,55,51,68,53,59,49,64,48,58,100,232,1792,148,78,93,62,62,47,35,43,52,22,26,35,5,0,0,1215,151,191,78,52,54,67,54,41,48,49,52,46,52,135,215,1412,250,108,83,51,52,69,60,29,31,22,39,87,67,128,12,2166,35,14,20,20,22,23,21,40,39,76,21,3,0,0,0,25,80,57,71,66,86,104,95,98,94,78,116,99,120,162,1149,96,100,88,113,112,94,94,137,93,94,106,102,112,158,449,552,615,229,165,133,132,98,92,91,107,78,129,586,41,4,0,0,49,51,5,6,15,14,17,24,46,57,58,62,47,39,44,1966,100,8,25,25,44,88,83,83,30,46,46,50,57,99,1153,563,551,83,48,33,24,23,25,25,51,53,442,1037,75,30,0,0,3,27,27,28,38,31,42,77,74,105,69,92,100,109,156,1522,7,35,33,37,49,61,71,109,104,78,93,102,99,145,459,1018,154,156,110,100,77,80,90,77,113,113,197,363,204,177,254,235,6,86,8,0,0,0,0,0,0,0,0,2,21,27,22,2328,100,0,0,0,0,0,0,0,1,9,34,29,26,71,146,2084,107,11,16,58,94,40,26,21,23,45,31,266,543,465,248,506,677,272,154,138,120,133,95,65,57,55,33,43,49,42,64,503,1037,179,154,172,96,60,35,42,50,58,37,57,52,57,74,340,1720,153,79,65,44,42,60,41,58,42,17,32,37,45,29,36,26,70,12,21,50,92,112,117,120,91,128,170,142,137,118,1094,111,40,89,193,165,138,162,175,142,87,93,105,105,182,392,321,1069,291,173,137,99,91,66,64,71,74,108,71,42,53,31,60,1238,162,97,77,65,59,74,60,58,67,45,48,44,33,65,308,1382,155,108,77,52,82,56,70,49,62,41,56,53,40,55,162,1772,199,99,93,78,44,50,24,43,45,21,13,13,5,1,0,249,202,198,175,176,150,171,196,172,156,120,118,84,76,101,156,502,396,275,281,196,140,147,141,95,71,74,55,51,28,37,11,1667,323,140,93,85,59,39,21,20,18,22,9,4,0,0,0,1832,43,21,41,17,31,26,9,12,29,32,22,45,30,46,264,1880,33,26,38,19,20,19,32,23,37,50,49,47,28,39,160,2020,111,44,82,36,38,39,30,41,26,9,13,11,0,0,0,1951,251,162,102,28,4,2,0,0,0,0,0,0,0,0,0,2368,124,5,3,0,0,0,0,0,0,0,0,0,0,0,0,2495,4,1,0,0,0,0,0,0,0,0,0,0,0,0,0},
{2500,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2500,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2500,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2500,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2500,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2500,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2500,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2500,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2500,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2500,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2500,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2500,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2500,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2500,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2500,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2500,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2500,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2500,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2500,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2500,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2500,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2500,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2500,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2500,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2500,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2500,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2500,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2500,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2500,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2500,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2500,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2500,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2500,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2500,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2500,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2500,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
};


	}
}
