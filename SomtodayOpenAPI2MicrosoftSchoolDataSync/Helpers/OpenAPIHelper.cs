using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using SomtodayOpenAPI2MicrosoftSchoolDataSync.Models;

namespace SomtodayOpenAPI2MicrosoftSchoolDataSync.Helpers
{
    internal class OpenAPIHelper
    {
        private SomOpenApiClient somOpenApiClient;
        public bool IsConnected = false;
        EventLogHelper eh = Program.eh;


        public OpenAPIHelper(string clientId, string clientSecret, string schoolUUID)
        {
            RestClient client = new RestClient("https://inloggen.somtoday.nl/oauth2/token?organisation=" + schoolUUID);
            RestRequest request = new RestRequest();
            request.AddHeader("content-type", "application/x-www-form-urlencoded");
            request.AddParameter("application/x-www-form-urlencoded", "grant_type=client_credentials&client_id=" + clientId + "&client_secret=" + clientSecret, ParameterType.RequestBody);
            try
            {
                RestResponse response = client.ExecutePost(request); //hier ontstaat een error indien Som niet bereikbaar is.
                if (response.IsSuccessful)
                {
                    dynamic data = JObject.Parse(response.Content); //hier ontstaat een error indien Som onderhoud heeft.
                    string accessToken = data.access_token;
                    HttpClient hc = new HttpClient();
                    hc.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                    somOpenApiClient = new SomOpenApiClient(hc);
                    IsConnected = true;
                }
            }
            catch
            {
                //LOG: connection failed
            }
        }



        internal List<VestigingModel> DownloadAllInfo(bool booleanFilterBylocation, string[] includedLocationCode, bool enableGuardianSync)
        {
            List<VestigingModel> result = new List<VestigingModel>();
            List<Vestiging> vestigingen = GetVestigingen();

            if (booleanFilterBylocation)
            {
                vestigingen = vestigingen.Where(v => includedLocationCode.Any(l => v.Afkorting.ToLower() == l.ToLower())).ToList();
            }

            foreach (Vestiging vestiging in vestigingen)
            {
                Console.WriteLine(vestiging.Naam);

                List<Lesgroep> lesgroepen = GetLesgroepen(vestiging);
                List<Medewerker> medewerkers = GetTeacherInfo(vestiging);
                List<Leerling> leerlingen = GetStudentInfo(vestiging);
                List<OuderVerzorger> ouders = new List<OuderVerzorger>();

                if (enableGuardianSync)
                {
                    ouders = GetGuardianInfo(vestiging);
                }

                if (lesgroepen.Count > 0 && medewerkers.Count > 0 && leerlingen.Count > 0 && (ouders.Count > 0 || !enableGuardianSync))
                {
                    VestigingModel vm = new VestigingModel()
                    {
                        Vestiging = vestiging,
                        Lesgroepen = lesgroepen,
                        Leerlingen = leerlingen,
                        Medewerkers = medewerkers,
                        OuderVerzorgers = ouders
                    };
                    result.Add(vm);
                }

            }
            return result;
        }


        private List<Vestiging> GetVestigingen()
        {
            List<Vestiging> vestigingen = new List<Vestiging>();
            VestigingResponse vestigingenResponse = somOpenApiClient.VestigingAsync(null, null).Result;
            return vestigingenResponse.Vestigingen.ToList();
        }

        private List<Lesgroep> GetLesgroepen(Vestiging vestiging)
        {
            bool getMoreLesgroepen = true;
            List<Lesgroep> _lesgroepen = new List<Lesgroep>();
            while (getMoreLesgroepen)
            {
                LesgroepResponse lesgroepenResponse = somOpenApiClient.LesgroepAsync(null, Peilschooljaar.HUIDIG, vestiging.Uuid, _lesgroepen.Count, 100, null, null).Result;

                if (lesgroepenResponse.Lesgroepen.Count != 0)
                {
                    _lesgroepen.AddRange(lesgroepenResponse.Lesgroepen);
                }
                else
                {
                    getMoreLesgroepen = false;
                }
            }
            return _lesgroepen;
        }


        private List<Leerling> GetStudentInfo(Vestiging vestiging)
        {
            bool getMoreLeerlingen = true;
            Console.WriteLine(string.Format("Leerlinggegevens opvragen..."));
            List<Leerling> _userLesgroepModel = new List<Leerling>();
            while (getMoreLeerlingen)
            {
                LeerlingResponse leerlingen = somOpenApiClient.LeerlingAsync(null, Peilschooljaar.HUIDIG, vestiging.Uuid, _userLesgroepModel.Count, 100, null, null).Result;
                if (leerlingen.Leerlingen.Count != 0)
                {
                    Console.WriteLine(_userLesgroepModel.Count);
                    _userLesgroepModel.AddRange(leerlingen.Leerlingen);
                }
                else
                {
                    getMoreLeerlingen = false;
                }
            }
            Console.Write(_userLesgroepModel.Count);
            Console.WriteLine(" leerlingen.");
            Console.WriteLine();
            return _userLesgroepModel;
        }

        private List<OuderVerzorger> GetGuardianInfo(Vestiging vestiging)
        {
            bool getMoreOuders = true;
            Console.WriteLine(string.Format("Oudergegevens opvragen..."));
            List<OuderVerzorger> _userLesgroepModel = new List<OuderVerzorger>();
            while (getMoreOuders)
            {
                OuderVerzorgerResponse ouders = somOpenApiClient.OuderVerzorgerAsync(null, Peilschooljaar.HUIDIG, vestiging.Uuid, _userLesgroepModel.Count, 100, null, null).Result;
                if (ouders.OuderVerzorgers.Count != 0)
                {
                    Console.WriteLine(_userLesgroepModel.Count);
                    _userLesgroepModel.AddRange(ouders.OuderVerzorgers);
                }
                else
                {
                    getMoreOuders = false;
                }
            }
            Console.Write(_userLesgroepModel.Count);
            Console.WriteLine(" ouders.");
            Console.WriteLine();
            return _userLesgroepModel;
        }

        private List<Medewerker> GetTeacherInfo(Vestiging vestiging)
        {
            bool getMoreMedewerkers = true;
            Console.WriteLine(string.Format("Medewerkergegevens opvragen..."));
            List<Medewerker> _userLesgroepModel = new List<Medewerker>();
            while (getMoreMedewerkers)
            {
                MedewerkerResponse medewerkers = somOpenApiClient.MedewerkerAsync(Peilschooljaar.HUIDIG, null, vestiging.Uuid, _userLesgroepModel.Count, 100, null, null).Result;
                if (medewerkers.Medewerkers.Count != 0)
                {
                    Console.WriteLine(_userLesgroepModel.Count);
                    _userLesgroepModel.AddRange(medewerkers.Medewerkers);
                }
                else
                {
                    getMoreMedewerkers = false;
                }
            }
            Console.Write(_userLesgroepModel.Count);
            Console.WriteLine(" medewerkers.");
            Console.WriteLine();
            return _userLesgroepModel;
        }
    }
}
