using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace JSONCarMakeTabulator
{
    class Program
    {
        public const string CarMakesUrl = "http://www.carqueryapi.com/api/0.3/?callback=?&cmd=getMakes";

        static void Main(string[] args)
        {
            CarMakesResponse carMakesResponse = CarMakesRequest(CarMakesUrl);
            Dictionary<string, CountryCarMakeCount> carMakeCounts = CollateMakes(carMakesResponse.Makes);

            if (TestCollateMakes(carMakesResponse.Makes, carMakeCounts.Values.ToArray<CountryCarMakeCount>()))
                Console.WriteLine("Car make data was successfully collated into counts of each country's common and uncommon makes");
            else
                Console.WriteLine("Collated country make data doesn't match general car make data");
            Console.ReadLine();

            try
            {
                string json = JsonConvert.SerializeObject(carMakeCounts.Values);
                File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\makeCounts.json", json); //Writes to user's Documents folder
            } catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

        }

        static bool TestCollateMakes(CarMake[] makes, CountryCarMakeCount[] countryCounts) 
        {
            try {
                CountryCarMakeCount[] newCountryCounts = CollateMakes(makes).Values.ToArray<CountryCarMakeCount>();

                if (countryCounts.Length != newCountryCounts.Length)
                    return false;

                for (int i = 0; i < countryCounts.Length; i++)
                    if (!countryCounts[i].Equals(newCountryCounts[i]))
                        return false;
            } catch { //Some error in converting makes or given country counts, indicates invalid data
                return false; 
            }

            return true;
            }

        static Dictionary<string, CountryCarMakeCount> CollateMakes(CarMake[] carMakes)
        {
            Dictionary<string, CountryCarMakeCount> carMakeCounts = new Dictionary<string, CountryCarMakeCount>();
            for (int i = 0; i < carMakes.Length; i++)   //Each iteration is a car make
            {
                CarMake currMake = carMakes[i];

                if (!carMakeCounts.ContainsKey(currMake.OriginCountry))//Make doesn't exist in dictionary
                {
                    carMakeCounts.Add(currMake.OriginCountry, new CountryCarMakeCount());
                    carMakeCounts[currMake.OriginCountry].name = currMake.OriginCountry; //Sets (country) name of the CountryCarMakeCount so that it can be
                }                                                                        //easily serialized

                if (currMake.IsCommon == 1) //Common make
                    carMakeCounts[currMake.OriginCountry].commonCount++;
                else                        //Uncommon make
                    carMakeCounts[currMake.OriginCountry].uncommonCount++;
            }
            return carMakeCounts;
        }

        static CarMakesResponse CarMakesRequest(string requestUrl)
        {
            try
            {
                HttpWebRequest request = WebRequest.Create(requestUrl) as HttpWebRequest;
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                        throw new Exception(String.Format(
                        "Server error (HTTP {0}: {1}).",
                        response.StatusCode,
                        response.StatusDescription));

                    StreamReader streamReader = new StreamReader(response.GetResponseStream());
                    string responseText = streamReader.ReadToEnd();
                    string responseJson = JsonTrim(responseText);

                    JsonSerializer jsonSerializer = new JsonSerializer();
                    CarMakesResponse carMakesResponse = JsonConvert.DeserializeObject<CarMakesResponse>(responseJson);
                    return carMakesResponse;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        

        //Trims any leading or trailing in a JSON string
        public static string JsonTrim(String rawJson)
        {
            int start = rawJson.IndexOf('{');
            int end = rawJson.LastIndexOf('}') - start + 1;
            return rawJson.Substring(start, end);
        }
    }

    #region data contract classes for serializing and deserializing car make data
    [DataContract]
    public class CarMakesResponse
    {
        [DataMember(Name = "Makes")]
        public CarMake[] Makes { get; set; }
    }

    [DataContract]
    public class CarMake
    {
        [DataMember(Name = "make_id")]
        public string Id { get; set; }
        [DataMember(Name = "make_display")]
        public string Name { get; set; }
        [DataMember(Name = "make_is_common")]
        public int IsCommon { get; set; }
        [DataMember(Name = "make_country")]
        public string OriginCountry { get; set; }
    }

    //Associates countries with the number of common and uncommon car makes 
    //that originate from them
    [DataContract]
    public class CountryCarMakeCount
    {
        [DataMember(Name = "country")]
        public string name;
        [DataMember(Name = "uncommon_makes")]
        public int uncommonCount;  
        [DataMember(Name = "common_makes")]
        public int commonCount;

        public override bool Equals(System.Object obj)
        {
            // If parameter is null return false.
            if (obj == null)
                return false;

            // If parameter cannot be cast to Point return false.
            CountryCarMakeCount castObj = obj as CountryCarMakeCount;
            if ((System.Object)castObj == null)
                return false;


            // Return true if the fields match:
            return (name == castObj.name && 
                uncommonCount == castObj.uncommonCount &&
                commonCount == castObj.commonCount);
        }
    }
    #endregion
}
