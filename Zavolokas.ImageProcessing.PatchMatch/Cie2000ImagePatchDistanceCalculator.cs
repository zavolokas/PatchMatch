using Zavolokas.Structures;

namespace Zavolokas.ImageProcessing.PatchMatch
{
    internal class Cie2000ImagePatchDistanceCalculator : ImagePatchDistanceCalculator
    {
        internal override unsafe double Calculate(int* destPatchImagePixelIndexesP, int* srcPatchImagePixelIndexesP, double maxDistance, double* destPixelsP, double* sourcePixelsP, ZsImage destImage, ZsImage srcImage, int patchLength)
        {
            // Constants for distance calculation

            const double BiggestDistance = 100.0;
            const double AvgDistance = BiggestDistance * .5;
            //constants needed for CIE2000
            const double Pi = System.Math.PI;
            const double C180DPi = 180d / Pi;
            const double CPid360 = Pi / 360d;
            const double CPid180 = Pi / 180.0;
            //Set weighting factors to 1
            const double k_L = 1.0d;
            const double k_C = 1.0d;
            const double k_H = 1.0d;

            double distance = 0;

            var destImageStride = destImage.Stride;
            var ercImageStride = srcImage.Stride;

            var destNumberOfComponents = destImage.NumberOfComponents;
            var srcNumberOfComponents = srcImage.NumberOfComponents;

            var destImageWidth = destImage.Width;
            var srcImageWidth = srcImage.Width;

            for (int i = 0; i < patchLength; i++)
            {
                var destPixelIndex = *(destPatchImagePixelIndexesP + i);
                var srcPixelIndex = *(srcPatchImagePixelIndexesP + i);
                if (destPixelIndex != -1 && srcPixelIndex != -1)
                {
                    int dstPointY = destPixelIndex / destImageWidth;
                    int dstPointX = destPixelIndex % destImageWidth;
                    var destColorIndex = destImageStride * dstPointY + dstPointX * destNumberOfComponents;

                    int srcPointY = srcPixelIndex / srcImageWidth;
                    int srcPointX = srcPixelIndex % srcImageWidth;
                    var sourceColorIndex = ercImageStride * srcPointY + srcPointX * srcNumberOfComponents;

                    #region Calculate deltaE using CIE2000
                    //TODO: opt - use lookup tables for math calcs

                    //Get colors' components
                    var L1 = *(destPixelsP + destColorIndex + 0);
                    var A1 = *(destPixelsP + destColorIndex + 1);
                    var B1 = *(destPixelsP + destColorIndex + 2);

                    var L2 = *(sourcePixelsP + sourceColorIndex + 0);
                    var A2 = *(sourcePixelsP + sourceColorIndex + 1);
                    var B2 = *(sourcePixelsP + sourceColorIndex + 2);

                    //Calculate Cprime1, Cprime2, Cabbar
                    double c_star_average_ab = (System.Math.Sqrt(A1 * A1 + B1 * B1) +
                                                System.Math.Sqrt(A2 * A2 + B2 * B2)) * 0.5;
                    double c_star_average_ab_pot7 = c_star_average_ab * c_star_average_ab * c_star_average_ab *
                                                    c_star_average_ab * c_star_average_ab * c_star_average_ab *
                                                    c_star_average_ab;

                    double G = 0.5d * (1 - System.Math.Sqrt(c_star_average_ab_pot7 / (c_star_average_ab_pot7 + 6103515625))); //25^7
                    double a1_prime = (1 + G) * A1;
                    double a2_prime = (1 + G) * A2;

                    double C_prime_1 = System.Math.Sqrt(a1_prime * a1_prime + B1 * B1);
                    double C_prime_2 = System.Math.Sqrt(a2_prime * a2_prime + B2 * B2);
                    //Angles in Degree.
                    double h_prime_1 = ((System.Math.Atan2(B1, a1_prime) * C180DPi) + 360) % 360d;
                    double h_prime_2 = ((System.Math.Atan2(B2, a2_prime) * C180DPi) + 360) % 360d;

                    double delta_L_prime = L2 - L1;
                    double delta_C_prime = C_prime_2 - C_prime_1;

                    double h_bar = System.Math.Abs(h_prime_1 - h_prime_2);
                    double delta_h_prime = 0;
                    double C_prime_1_Times_C_prime_2 = C_prime_1 * C_prime_2;
                    if (C_prime_1_Times_C_prime_2 != 0)
                    {
                        if (h_bar <= 180d)
                        {
                            delta_h_prime = h_prime_2 - h_prime_1;
                        }
                        else if (h_bar > 180d && h_prime_2 <= h_prime_1)
                        {
                            delta_h_prime = h_prime_2 - h_prime_1 + 360.0;
                        }
                        else
                        {
                            delta_h_prime = h_prime_2 - h_prime_1 - 360.0;
                        }
                    }
                    double delta_H_prime = 2 * System.Math.Sqrt(C_prime_1_Times_C_prime_2) * System.Math.Sin(delta_h_prime * CPid360);

                    // Calculate CIEDE2000
                    double L_prime_average = (L1 + L2) * 0.5;
                    double C_prime_average = (C_prime_1 + C_prime_2) * 0.5;

                    //Calculate h_prime_average

                    double h_prime_average = 0;
                    if (C_prime_1_Times_C_prime_2 != 0)
                    {
                        if (h_bar <= 180d)
                        {
                            h_prime_average = (h_prime_1 + h_prime_2) * 0.5;
                        }
                        else if (h_bar > 180d && (h_prime_1 + h_prime_2) < 360d)
                        {
                            h_prime_average = (h_prime_1 + h_prime_2 + 360d) * 0.5;
                        }
                        else
                        {
                            h_prime_average = (h_prime_1 + h_prime_2 - 360d) * 0.5;
                        }
                    }
                    double L_prime_average_minus_50_square = (L_prime_average - 50) * (L_prime_average - 50);

                    double S_L = 1 +
                                 ((.015d * L_prime_average_minus_50_square) /
                                  System.Math.Sqrt(20 + L_prime_average_minus_50_square));
                    double S_C = 1 + .045d * C_prime_average;
                    double S_H = 1 + (1 - .17 * System.Math.Cos((h_prime_average - 30) * CPid180)
                                      + .24 * System.Math.Cos((h_prime_average * 2) * CPid180)
                                      + .32 * System.Math.Cos((h_prime_average * 3 + 6) * CPid180)
                                      - .2 * System.Math.Cos((h_prime_average * 4 - 63) * CPid180)) *
                                 C_prime_average * .015;

                    double C_prime_average_pot_7 = C_prime_average * C_prime_average * C_prime_average *
                                                   C_prime_average * C_prime_average * C_prime_average *
                                                   C_prime_average;
                    double R_T =
                        -System.Math.Sin((60 *
                                          System.Math.Exp(-1 * ((h_prime_average - 275) * 0.04) *
                                                          ((h_prime_average - 275) * 0.04))) * CPid180) *
                        (2 * System.Math.Sqrt(C_prime_average_pot_7 / (C_prime_average_pot_7 + 6103515625)));

                    double delta_L_prime_div_k_L_S_L = delta_L_prime / (S_L * k_L);
                    double delta_C_prime_div_k_C_S_C = delta_C_prime / (S_C * k_C);
                    double delta_H_prime_div_k_H_S_H = delta_H_prime / (S_H * k_H);

                    double CIEDE2000 = System.Math.Sqrt(
                        delta_L_prime_div_k_L_S_L * delta_L_prime_div_k_L_S_L
                        + delta_C_prime_div_k_C_S_C * delta_C_prime_div_k_C_S_C
                        + delta_H_prime_div_k_H_S_H * delta_H_prime_div_k_H_S_H
                        + R_T * delta_C_prime_div_k_C_S_C * delta_H_prime_div_k_H_S_H
                        );

                    distance += CIEDE2000;
                    #endregion
                }
                else
                {
                    distance += BiggestDistance; // AvgDistance;
                }

                if (distance > maxDistance)
                    break;
            }

            return distance;
        }
    }
}