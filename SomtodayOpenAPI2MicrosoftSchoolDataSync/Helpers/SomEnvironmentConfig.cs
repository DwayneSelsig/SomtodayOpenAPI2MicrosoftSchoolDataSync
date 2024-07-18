using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SomtodayOpenAPI2MicrosoftSchoolDataSync.Helpers
{
    public class SomEnvironmentConfig
    {
        public string Url { get; private set; }
        public string LoginUrl { get; private set; }
        public string SelectedEnvironment { get; private set; }

        private SomEnvironmentConfig(string url, string loginUrl, string selectedEnvironment)
        {
            Url = url;
            LoginUrl = loginUrl;
            SelectedEnvironment = selectedEnvironment;
        }

        /// <summary>
        /// Nightly omgeving binnen Somtoday oatp landschap. Alleen intern beschikbaar.
        /// </summary>
        public static readonly SomEnvironmentConfig Nightly = new SomEnvironmentConfig(
            "http://api.nightly.somtoday.build/rest/v1",
            "https://inloggen.nightly.somtoday.nl/oauth2/token?organisation=",
            "Nightly"
        );

        /// <summary>
        /// Acceptatie omgeving binnen Somtoday oatp landschap. Toegang op aanvraag.
        /// </summary>
        public static readonly SomEnvironmentConfig Acceptatie = new SomEnvironmentConfig(
            "https://api.acceptatie.somtoday.nl/rest/v1",
            "https://inloggen.acceptatie.somtoday.nl/oauth2/token?organisation=",
            "Acceptatie"
        );

        /// <summary>
        /// Test omgeving binnen Somtoday oatp landschap. Toegang op aanvraag. Achterliggende data wordt maandelijks ververst.
        /// </summary>
        public static readonly SomEnvironmentConfig Test = new SomEnvironmentConfig(
            "https://api.test.somtoday.nl/rest/v1",
            "https://inloggen.test.somtoday.nl/oauth2/token?organisation=",
            "Test"
        );

        /// <summary>
        /// Somtoday Productie omgeving.
        /// </summary>
        public static readonly SomEnvironmentConfig Prod = new SomEnvironmentConfig(
            "https://api.somtoday.nl/rest/v1",
            "https://inloggen.somtoday.nl/oauth2/token?organisation=",
            "Productie"
        );
    }
}
