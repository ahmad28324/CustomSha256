namespace CustomSha256
{
    using System;
    using System.Security.Cryptography;
    using System.Text;

    class Program
    {
        private const string HelloString = @"Hello";
        private const string LoremIpsumString = @"  nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";

        static void Main(string[] args)
        {
            var stringBytes = Encoding.UTF8.GetBytes(LoremIpsumString);
            var hash = CustomSha256.ComputeHash(stringBytes);
           // Console.WriteLine(stringBytes.Length);
        

                  string hashString = string.Empty;

             //======================================
            // for Proof Of Work
            hash = CustomSha256.ComputeHash(stringBytes);
            byte difficulty = 5;
            byte[] challenge = null;
            var watch1 = new System.Diagnostics.Stopwatch();
            Proofofwork POW = new Proofofwork(hash, difficulty, challenge);
            Console.WriteLine($"Searching for a hush with difficulty {difficulty}");
            bool find;
            byte[] solution;
            bool POW_result;
            watch1.Start();
            find = POW.FindSolution(stringBytes);
            watch1.Stop();
            Console.WriteLine($"Execution Time: {watch1.ElapsedMilliseconds} ms");
            solution = POW.right_hush_;
            POW_result = POW.VerifySolution(POW.right_hush_);
            Console.WriteLine("POW result :" + POW_result);
            Console.WriteLine("Difficulty :" + POW.Difficulty_);

            hash = POW.solution_number_;
            hashString = string.Empty;
            foreach (var @byte in hash)
            {
                hashString += string.Format("{0:X2}", @byte);
            }
            Console.WriteLine("solution :" + hashString);
            // for displaying
            var str = POW.right_hush_;
            string strString = string.Empty;
            foreach (var @byte in str)
            {
                strString += string.Format("{0:X2}", @byte);
            }
            Console.WriteLine("Challenge :" + strString);

            //=================================
            //=============================================================
            // to calculate the average time for dificulty in 1000 
            /*double time = 0;
            int repeate_num = 1000;
            var watch2 = new System.Diagnostics.Stopwatch();
            watch2.Start();
            int count=0;
            for (int i=0; i<repeate_num;i++)
            {
                challenge = null;
                POW = new Proofofwork(hash, difficulty, challenge);
                find = POW.FindSolution(stringBytes);
                Console.WriteLine(i);
            }

            watch2.Stop();
            time = time + watch2.ElapsedMilliseconds;
            time = time/repeate_num;
             Console.WriteLine($"Execution Time: {time} ms withe dificulty = {POW.Difficulty_} and repeat num = {repeate_num}");
            
              solution = POW.right_hush_;
            POW_result = POW.VerifySolution(POW.right_hush_);
            Console.WriteLine("POW result :" + POW_result);
            Console.WriteLine("Difficulty :" + POW.Difficulty_);

            hash = POW.solution_number_;
            hashString = string.Empty;
            foreach (var @byte in hash)
            {
                hashString += string.Format("{0:X2}", @byte);
            }
            Console.WriteLine("solution :" + hashString);
            // for displaying
             str = POW.right_hush_;
             strString = string.Empty;
            foreach (var @byte in str)
            {
                strString += string.Format("{0:X2}", @byte);
            }
            Console.WriteLine("Challenge :" + strString);

             
             */

            //=============================================================
            //=================================

        }

    }
}
