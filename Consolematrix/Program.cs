using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;


namespace Consolematrix
{
    enum SegmentType
    {
        row,
        col
    }

   public class MatrixData
    {
        public int[] Value { get; set; }
        public string Cause { get; set; }
        public bool Success { get; set; }
    }

    public class MatrixDataValidation
    {
        public string Value { get; set; }
        public string Cause { get; set; }
        public bool Success { get; set; }
    }

    class Program
    {
        static string baseUrl = "https://recruitment-test.investcloud.com";
        static int matrixSize = 1000;
        static HttpClient client = new HttpClient();

     
        static void Main(string[] args)
        {
           
           
            int[][] A = new int[matrixSize][];
            int[][] B = new int[matrixSize][];
            int[][] C = new int[matrixSize][];

            Stopwatch stopwatch = Stopwatch.StartNew();
            InitDataSet(matrixSize);
           // populateMatrix(ref A, ref B, matrixSize);

            stopwatch.Stop();
            Console.WriteLine("Time to load matrix="+ stopwatch.ElapsedMilliseconds + " milli sec");
            stopwatch = Stopwatch.StartNew();          
            
            //populate B matrix column data
            Parallel.For(0, matrixSize, i => {
                B[i] = GetMatrixData(SegmentType.col, i, "B").Result;
            });
          
            stopwatch.Stop();
            Console.WriteLine("Time to populate B matrix=" + stopwatch.ElapsedMilliseconds + " milli sec");
            stopwatch = Stopwatch.StartNew();
         
            Parallel.For(0, matrixSize, rowIdx =>
            {

                C[rowIdx] = new int[matrixSize];
                A[rowIdx] = GetMatrixData(SegmentType.row, rowIdx, "A").Result;

                Parallel.For(0, matrixSize, colIdx =>
                {

                    C[rowIdx][colIdx] = 0;

                    //for (int i = 0; i < matrixSize; i++)
                    Parallel.For(0, matrixSize, idx =>
                    {
                        // C[rowIdx][colIdx] = C[rowIdx][colIdx] + A[rowIdx][i] * B[i][colIdx];
                        C[rowIdx][colIdx] = C[rowIdx][colIdx] + A[rowIdx][idx] * B[colIdx][idx];
                    });
                });

            });
            

           

            stopwatch.Stop();
            Console.WriteLine("Time to multiply C matrix=" + stopwatch.ElapsedMilliseconds + " milli sec");


            string arrayToString = string.Join("", C.SelectMany(iRow=>iRow));
           
           Console.Write("Result="+ ValdiateResult(arrayToString));
            System.Console.ReadKey();
        }

        static string ValdiateResult(string data)
        {
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(data);
            byte[] hash = md5.ComputeHash(inputBytes);
            HttpContent content = new StringContent(System.Text.Encoding.UTF8.GetString(hash), Encoding.UTF8, "application/json");
            HttpResponseMessage response = client.PostAsync($"{baseUrl}/api/numbers/validate", content).Result;
            MatrixDataValidation svcData = JsonConvert.DeserializeObject<MatrixDataValidation>(response.Content.ReadAsStringAsync().Result);
           
            return svcData.Value;

        }
        static void InitDataSet(int size)
        {
       
            HttpResponseMessage response = client.GetAsync($"{baseUrl}/api/numbers/init/{size}").Result;
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("service reqeust failed with error code="+ response.StatusCode);
            }
            
        }
        static async Task<int[]> GetMatrixData(SegmentType type, int idx, string dataset)
        {
            
            int[] result = new int[matrixSize];

            HttpResponseMessage response = await client.GetAsync($"{baseUrl}/api/numbers/{dataset}/{type.ToString()}/{idx}");
            if (response.IsSuccessStatusCode)
            {
                MatrixData svcData = JsonConvert.DeserializeObject<MatrixData>(response.Content.ReadAsStringAsync().Result);
                if (svcData.Success)
                {
                    result =  (int[]) svcData.Value;
                }
            }
            return result;
        }
        
    }
}
