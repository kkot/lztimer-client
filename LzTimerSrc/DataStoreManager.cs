using System;
using System.IO;

namespace kkot.LzTimer
{
    class DataStoreManager
    {
        public static void ReadAndUpdateNumOfSeconds(bool workingMode, int secondsAfterLastActivity, String date, out int numOfWorkSeconds, out int numOfFunSeconds)
        {
            String filename = date + ".txt";
            try
            {
                string readContent = File.ReadAllText(filename);
                String[] splitedContent = readContent.Split("\n".ToCharArray());
                numOfWorkSeconds = int.Parse(splitedContent[0]);
                numOfFunSeconds = int.Parse(splitedContent[1]);
            }
            catch (Exception exception)
            {
                numOfWorkSeconds = 0;
                numOfFunSeconds = 0;
            }

            if (workingMode)
            {
                numOfWorkSeconds += secondsAfterLastActivity;
            }
            else
            {
                numOfFunSeconds += secondsAfterLastActivity;
            }
            File.WriteAllLines(filename, new string[] { numOfWorkSeconds.ToString(), numOfFunSeconds.ToString() });
        }
    }
}
