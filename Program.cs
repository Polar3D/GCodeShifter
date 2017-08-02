using System;
using System.Text;
using System.IO;

namespace GCodeShifter
{

    class Program
    {
        static double y_offset;
        static double z_offset;
        static double x_original = 0;
        static double y_original = 0;
        static double totalZOffset = 0;
        static double totalYOffset = 0;
        static double SwapLayers = 0;
        static double min_x_value = 0;
        static double min_y_value = 0;

        // defaults
        static double Angle = 35.0;
        static double Layer = .2;

        static void Main(string[] args)
        {

            string inputFile = args[0];
            string outputFile = args[1];
            string xoffsetLength = "";
            string yoffsetLength = "";


            double currentOffset = 0.0;

            // if we have an X offset, record it
            if (args.Length > 2)
            {
                xoffsetLength = args[2];
                Double.TryParse(xoffsetLength, out x_original);
            };

            // if we have a Y offset, record it
            if (args.Length > 3)
            {
                yoffsetLength = args[3];
                Double.TryParse(yoffsetLength, out y_original);
            }

            // if we have a angle, record it
            if (args.Length > 4)
            {
                string newAngle = args[4];
                Double.TryParse(newAngle, out Angle);

                Console.Write("Angle: ");
                Console.WriteLine(Angle.ToString());

            }

            // if we have a layer height, record it
            if (args.Length > 5)
            {
                string newLayer = args[5];
                Double.TryParse(newLayer, out Layer);

                Console.Write("Layer Height: ");
                Console.WriteLine(Layer.ToString());

            }

            // if we are swapping axis, record it
            if (args.Length > 6)
            {
                string swapAxis = args[6];
                Double.TryParse(swapAxis, out SwapLayers);

                Console.Write("Swapping Layers: ");
                Console.WriteLine(SwapLayers.ToString());

            }

            // calculate triangle side adjustments
            z_offset = Layer / Math.Cos(Angle * (Math.PI / 180)) - Layer;
            y_offset = Layer * Math.Tan(Angle * (Math.PI / 180));

            currentOffset = y_offset;

            Console.Write("Z Offset: ");
            Console.WriteLine(z_offset.ToString());

            Console.Write("Y Offset: ");
            Console.WriteLine(y_offset.ToString());

            // calulate axis offset
            try
            {

                using (StreamReader sr = File.OpenText(inputFile))
                {
                    using (StreamWriter sw = new StreamWriter(outputFile))
                    {
                        string s = String.Empty;
                        while ((s = sr.ReadLine()) != null)
                        {
                            // sw.WriteLine(ProcessLine(s.TrimStart(), sw));
                            // determine min x value
                            // determine min y value
                            string[] temp;
                            string lineData = s.TrimStart();
                            temp = s.Split(Char.Parse(" "));

                            if ((temp[0] == "G0" || temp[0] == "G1"))
                            {
                                if (lineData.IndexOf("X") > 0)
                                {
                                    for (int segment = 0; segment < temp.Length; segment++)
                                    {
                                        if (temp[segment].StartsWith("X"))
                                        {
                                            min_x_value = double.Parse(temp[segment].Substring(1));
                                        }
                                    }
                                }
                                if (lineData.IndexOf("Y") > 0)
                                {
                                    for (int segment = 0; segment < temp.Length; segment++)
                                    {
                                        if (temp[segment].StartsWith("Y"))
                                        {
                                            min_y_value = double.Parse(temp[segment].Substring(1));
                                        }
                                    }
                                }
                            }
                            if (SwapLayers != 0)
                            {
                                if (min_x_value > 0) break;
                            }
                            else
                            {
                                if (min_y_value > 0) break;
                            }
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
            }


            try
            {

                using (StreamReader sr = File.OpenText(inputFile))
                {
                    using (StreamWriter sw = new StreamWriter(outputFile))
                    {
                        string s = String.Empty;
                        while ((s = sr.ReadLine()) != null)
                        {
                            sw.WriteLine(ProcessLine(s.TrimStart(), sw));
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
            }
            // close files
            Console.WriteLine("GCodeShifter Complete");

            //}
        }

        static string ProcessLine(string lineData, StreamWriter sw)
        {
            string[] temp;
            temp = lineData.Split(Char.Parse(" "));

            StringBuilder tempData = new StringBuilder(temp.Length);

            // if the first parameter is a G0 with a trailing Z
            // then this is a Z height change - note, need to fix this for other slicers
            if (temp[0] == "G0" && (lineData.IndexOf("Z") > 0))
            {
                // read in the current z layer height
                double currentZ = double.Parse(lineData.Substring(lineData.IndexOf("Z") + 1, (lineData.Length - lineData.IndexOf("Z") - 1)));

                totalZOffset = totalZOffset + z_offset;

                // then read the offset value to shift the layer
                currentZ = currentZ + totalZOffset;

                lineData = lineData.Substring(0, lineData.IndexOf("Z") + 1) + currentZ.ToString("0.######");
                temp = lineData.Split(Char.Parse(" "));

                // OH... I have to trigger the Y offset here
                totalYOffset = currentZ * Math.Tan(Angle * (Math.PI / 180));
            }

            if (totalYOffset != 0.0)
            {
                // if we are on a G0 or G1 line (no Z!)
                if ((temp[0] == "G0" || temp[0] == "G1"))
                {
                    if (lineData.IndexOf("X") > 0)
                    {
                        if (lineData.IndexOf("Y") > 0)
                        {
                            bool xFixed = false;
                            bool yFixed = false;

                            for (int segment = 0; segment < temp.Length; segment++)
                            {
                                if (temp[segment].StartsWith("X") && !xFixed)
                                {
                                    double xValue = double.Parse(temp[segment].Substring(1));
                                    if (SwapLayers != 0)
                                        temp[segment] = "X" + (xValue + x_original + totalYOffset - min_x_value).ToString("0.######");
                                    else
                                        temp[segment] = "X" + (xValue + x_original).ToString("0.######");

                                    xFixed = !xFixed;
                                }

                                if (temp[segment].StartsWith("Y") && !yFixed)
                                {
                                    double yValue = double.Parse(temp[segment].Substring(1));
                                    if (SwapLayers == 0)
                                        temp[segment] = "Y" + (yValue + y_original + totalYOffset - min_y_value).ToString("0.######");
                                    else
                                        temp[segment] = "Y" + (yValue + y_original).ToString("0.######");

                                    yFixed = !yFixed;
                                }
                            }

                            lineData = "";
                            foreach (string segment in temp)
                            {
                                lineData = lineData + segment + " ";
                            }

                        }
                    }
                }
            }
            return lineData;
        }
    }
}
