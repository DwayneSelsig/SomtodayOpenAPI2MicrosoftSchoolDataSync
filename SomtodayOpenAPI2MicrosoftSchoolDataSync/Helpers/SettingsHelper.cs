using SomtodayOpenAPI2MicrosoftSchoolDataSync.Helpers;
using SomtodayOpenAPI2MicrosoftSchoolDataSync;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SomtodayOpenAPI2MicrosoftSchoolDataSync.Models;
using System.Linq.Expressions;


namespace SomtodayOpenAPI2MicrosoftSchoolDataSyncV2.Helpers
{
    public class SettingsHelper
    {
        public static readonly string OutputFormatUsernameTeacher = BuildUsernameFormat(ConfigurationManager.AppSettings["OutputFormatUsernameTeacher"]);
        public static readonly string OutputFormatUsernameStudent = BuildUsernameFormat(ConfigurationManager.AppSettings["OutputFormatUsernameStudent"]);


        EventLogHelper eh = Program.eh;

        private static string BuildUsernameFormat(string configuredFormat)
        {
            string format = string.IsNullOrWhiteSpace(configuredFormat) ? "Emailadres" : configuredFormat;
            return format.StartsWith("{user.") && format.EndsWith("}") ? format : "{user." + format + "}";
        }

        internal bool ValidateUsernameFormat()
        {
            bool success = true;
            Medewerker medewerkerUser = new Medewerker() { Emailadres = "testnaam" };
            try
            {
                ReplaceTeacherUserProperty(OutputFormatUsernameTeacher, medewerkerUser);
            }
            catch (Exception ex)
            {
                success = false;
                eh.WriteLog(string.Format("OutputFormatUsernameTeacher onjuist: {0}", ex.Message), Microsoft.Extensions.Logging.LogLevel.Error, 500);
            }

            Leerling leerlingUser = new Leerling() { Emailadres = "testnaam" };

            try
            {
                ReplaceStudentUserProperty(OutputFormatUsernameStudent, leerlingUser);
            }
            catch (Exception ex)
            {
                success = false;
                eh.WriteLog(string.Format("OutputFormatUsernameStudent onjuist: {0}", ex.Message), Microsoft.Extensions.Logging.LogLevel.Error, 500);
            }
            return success;
        }

        internal string ReplaceTeacherProperty(string format, Medewerker userobj)
        {
            return ReplaceTeacherUserProperty(format, userobj);
        }

        internal string ReplaceStudentProperty(string format, Leerling userobj)
        {
            return ReplaceStudentUserProperty(format, userobj);
        }


        private static string ReplaceTeacherUserProperty(string value, Medewerker userobj)
        {
            return Regex.Replace(value, @"{(?<exp>[^}]+)}", match =>
            {
                var p = Expression.Parameter(typeof(Medewerker), "user");
                var e = System.Linq.Dynamic.Core.DynamicExpressionParser.ParseLambda(new[] { p }, null, match.Groups["exp"].Value);
                return (e.Compile().DynamicInvoke(userobj) ?? "").ToString();
            });
        }
        private static string ReplaceStudentUserProperty(string value, Leerling userobj)
        {
            return Regex.Replace(value, @"{(?<exp>[^}]+)}", match =>
            {
                var p = Expression.Parameter(typeof(Leerling), "user");
                var e = System.Linq.Dynamic.Core.DynamicExpressionParser.ParseLambda(new[] { p }, null, match.Groups["exp"].Value);
                return (e.Compile().DynamicInvoke(userobj) ?? "").ToString();
            });
        }
    }
}
