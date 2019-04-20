using System;
using System.Collections.Generic;
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
    class Program
    {
        static void Main(string[] args)
        {
            var jsonURL = @"https://data.melbourne.vic.gov.au/resource/imwx-szwr.json?$order=:id&$limit=100000&$offset=0";
            var ds2URL = @"https://data.melbourne.vic.gov.au/resource/44kh-ty54.json?$order=:id&$limit=100000&$offset=0";
            //var geoString = GetJsonData(jsonURL);
            // var geoString2 = GetJsonData(ds2URL);

            // Data source doesn't have post codes so I've ripped some from the web
            // and will insert into each record
            var zipString = GetZipData(@"Data\postalAreas.json");

            // // Map to intermediary model for renaming and discarding of properties
            // var addresses = Address.FromJson(geoString);
            // List<AddressDTO> addressList = MapToDTO(addresses, zipString);

            // using (StreamWriter outFile = File.CreateText(@"Data\AddressDTO.json"))
            // {
            //     JsonSerializer serializer = new JsonSerializer();
            //     serializer.Serialize(outFile, addressList);
            // }

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

                    tempDTO.City = address.ClueSmallArea;
                    tempDTO.Street = address.StreetName;
                    tempDTO.State = "Victoria";
                    var tempSuburb = Regex.Replace(address.ClueSmallArea, @" \((.*?)\)", "");
                    tempDTO.Zip = GetZip(ZipString, tempSuburb.ToUpper());
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
