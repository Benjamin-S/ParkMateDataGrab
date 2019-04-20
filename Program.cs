using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using DataGrab.Models;
using DataGrab.Models.DS2;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DataGrab
{
    class AddressEqualityComparer : IEqualityComparer<AddressDTO>
    {
        public bool Equals(AddressDTO x, AddressDTO y)
        {
            return x.Street == y.Street;
        }

        public int GetHashCode(AddressDTO obj)
        {
            return obj.Street.GetHashCode();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // Data source doesn't have post codes so I've ripped some from the web
            // and will insert into each record
            var zipString = GetZipData(@"Data\postalAreas.json");

            //MergeDatasets(@"Data\AddressDTO2.json", @"Data\AddressDTO.json");
            ValidateData(@"Data\MergedData.json");
            // Debug.WriteLine(RecordCount(@"Data\Validated.json"));

            // string dataSet;
            // using (StreamReader streamReader = new StreamReader(@"Data\MergedData.json", Encoding.UTF8))
            // {
            //     dataSet = streamReader.ReadToEnd();
            // }
            // JArray ja1 = JArray.Parse(dataSet);
            // List<string> data = new List<string>();
            // foreach(var ja in ja1)
            // {
            //     data.Add(ja.ToString());
            // }

            // Debug.WriteLine(data.Count());
            // Debug.WriteLine(RemoveDuplicates(data).Count());


            // ValidateData(@"Data\MergedData.json");

            // DataSet2(zipString);

        }


        static int RecordCount(string file)
        {
            string dataSet;
            using (StreamReader streamReader = new StreamReader(file, Encoding.UTF8))
            {
                dataSet = streamReader.ReadToEnd();
            }
            JArray ja1 = JArray.Parse(dataSet);

            return ja1.Count();
        }

        static void ValidateData(string ds1)
        {
            string dataSet;
            using (StreamReader streamReader = new StreamReader(ds1, Encoding.UTF8))
            {
                dataSet = streamReader.ReadToEnd();
            }
            JArray ja1 = JArray.Parse(dataSet);

            List<AddressDTO> distinct = ja1.ToObject<List<AddressDTO>>();
            var result = distinct.Distinct(new AddressEqualityComparer());

            using (StreamWriter outFile = File.CreateText(@"Data\Validated.json"))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(outFile, result);
            }

        }


        public static List<string> RemoveDuplicates(List<string> addresses)
        {
            var result = new List<string>();
            var set = new HashSet<string>();

            for (int i = 0; i < addresses.Count; i++)
            {
                if (!set.Contains(addresses[i]))
                {
                    result.Add(addresses[i]);
                    set.Add(addresses[i]);
                }
            }
            return result;
        }

        static void MergeDatasets(string ds1, string ds2)
        {
            string dataSet;
            using (StreamReader streamReader = new StreamReader(ds1, Encoding.UTF8))
            {
                dataSet = streamReader.ReadToEnd();
            }
            JArray ja1 = JArray.Parse(dataSet);
            Debug.WriteLine("Dataset A:" + ja1.Count());

            string dataSet2;
            using (StreamReader streamReader = new StreamReader(ds2, Encoding.UTF8))
            {
                dataSet2 = streamReader.ReadToEnd();
            }
            JArray ja2 = JArray.Parse(dataSet2);
            Debug.WriteLine("Dataset B:" + ja2.Count());

            ja1.Merge(ja2, new JsonMergeSettings
            {
                MergeArrayHandling = MergeArrayHandling.Union
            });

            Debug.WriteLine("Dataset Merge:" + ja1.Count());

            using (StreamWriter outFile = File.CreateText(@"Data\MergedData.json"))
            {
                using (JsonTextWriter writer = new JsonTextWriter(outFile))
                {
                    ja1.WriteTo(writer);
                }
            }
        }

        static string GetZipData(string zipFile)
        {
            string zipString = null;
            FileStream zipStream = new FileStream(zipFile, FileMode.Open);
            using (StreamReader reader = new StreamReader(zipStream))
            {
                zipString = reader.ReadToEnd();
            }
            return zipString;
        }

        static string GetJsonData(string Url)
        {
            string geoString = null;
            using (var webClient = new WebClient())
            {
                geoString = webClient.DownloadString(Url);
            }
            return geoString;
        }


        static public void DataSet1(String zipString)
        {
            var jsonURL = @"https://data.melbourne.vic.gov.au/resource/imwx-szwr.json?$order=:id&$limit=100000&$offset=0";
            var geoString = GetJsonData(jsonURL);

            // Map to intermediary model for renaming and discarding of properties
            var addresses = Address.FromJson(geoString);
            List<AddressDTO> addressList = MapToDTO(addresses, zipString);

            using (StreamWriter outFile = File.CreateText(@"Data\AddressDTO.json"))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(outFile, addressList);
            }
        }

        static public void DataSet2(String zipString)
        {
            // var ds2URL = @"https://data.melbourne.vic.gov.au/resource/44kh-ty54.json?$order=:id&$limit=100000&$offset=0";
            // var geoString2 = GetJsonData(ds2URL);
            string geoString2;
            using (StreamReader streamReader = new StreamReader(@"Data\44kh-ty54.json", Encoding.UTF8))
            {
                geoString2 = streamReader.ReadToEnd();
            }

            // Map to intermediary model for renaming and discarding of properties
            var addresses = AddressDS2.FromJson(geoString2);
            List<AddressDTO> addressList = MapToDTO2(addresses, zipString);

            using (StreamWriter outFile = File.CreateText(@"Data\AddressDTO2.json"))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(outFile, addressList);
            }
        }
        static List<AddressDTO> MapToDTO(List<Address> addresses, string ZipString)
        {
            List<AddressDTO> addressDTO = new List<AddressDTO>();

            foreach (var address in addresses)
            {
                // Theres only 6 addresses that lack a suburb
                if (address.Suburb != null)
                {
                    AddressDTO tempDTO = new AddressDTO();

                    StringBuilder sb = new StringBuilder();
                    sb.Append(address.StreetNo);
                    sb.Append(" ");
                    sb.Append(address.StrName);
                    var streetAddress = sb.ToString();

                    tempDTO.City = address.Suburb;
                    tempDTO.Street = streetAddress;
                    tempDTO.State = "Victoria";
                    tempDTO.Zip = GetZip(ZipString, address.Suburb.ToUpper());
                    tempDTO.Latitude = Convert.ToDouble(address.Latitude);
                    tempDTO.Longitude = Convert.ToDouble(address.Longitude);

                    addressDTO.Add(tempDTO);
                }
            }

            return addressDTO;
        }


        static List<AddressDTO> MapToDTO2(List<AddressDS2> addresses, string ZipString)
        {
            List<AddressDTO> addressDTO = new List<AddressDTO>();

            foreach (var address in addresses)
            {
                // Theres only 6 addresses that lack a suburb
                if (address.ClueSmallArea != null)
                {
                    AddressDTO tempDTO = new AddressDTO();

                    StringBuilder sb = new StringBuilder();

                    string suburb = "";
                    if(address.ClueSmallArea.Contains('('))
                    {
                        suburb = Regex.Replace(address.ClueSmallArea, @" \((.*?)\)", "");
                    }
                    else
                    {
                     suburb = address.ClueSmallArea;   
                    }

                    tempDTO.City = address.ClueSmallArea;
                    tempDTO.Street = address.StreetName;
                    tempDTO.State = "Victoria";
                    tempDTO.Zip = GetZip(ZipString, suburb.ToUpper());
                    tempDTO.Latitude = Convert.ToDouble(address.YCoordinate);
                    tempDTO.Longitude = Convert.ToDouble(address.XCoordinate);

                    addressDTO.Add(tempDTO);
                }
            }

            return addressDTO;
        }

        static string GetZip(string fileString, string Suburb)
        {
            JArray array = JArray.Parse(fileString);
            if (array.Count > 0)
            {
                return array
                    .Where(jt => (string)jt["Suburb"] == Suburb)
                    .Select(jt => (string)jt["Zip"])
                    .First();
            }
            return null;
        }
    }
}
