using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SomtodayOpenAPI2MicrosoftSchoolDataSync.Helpers
{
    public static class BusinessLogicHelper
    {
        public static string NormaliseerTelefoonnummerNaarE164(string invoer)
        {
            string genormaliseerd = string.Empty;

            if (!string.IsNullOrEmpty(invoer))
            {
                // Verwijder alle niet-numerieke tekens behalve het plusteken
                genormaliseerd = Regex.Replace(invoer, @"[^\d+]", "");

                // Als het nummer 9 cijfers bevat en niet met '0' begint, aannemen dat het een mobiel nummer zonder voorloopnul is
                if (genormaliseerd.Length == 9 && !genormaliseerd.StartsWith("0"))
                {
                    // Voeg de voorloopnul toe voor een mobiel nummer
                    genormaliseerd = "0" + genormaliseerd;
                }

                // Als het nummer begint met 00, vervang dit door een +
                if (genormaliseerd.StartsWith("00"))
                {
                    genormaliseerd = "+" + genormaliseerd.Substring(2); // Vervang de '00' door '+'
                }
                else if (genormaliseerd.StartsWith("0"))
                {
                    // Als het nummer begint met een 0, vervang deze door de Nederlandse landcode +31
                    genormaliseerd = "+31" + genormaliseerd.Substring(1);
                }
                else if (!genormaliseerd.StartsWith("+"))
                {
                    // Als het nummer niet met een '+' begint en ook niet met '00' of '0', is het waarschijnlijk incorrect
                    //throw new ArgumentException("Telefoonnummer moet beginnen met '0', '00' of '+'");
                    genormaliseerd = "";
                }

                // Valideer of het genormaliseerde nummer voldoet aan de E.164-standaard (max 15 cijfers)
                string nummerZonderPlus = genormaliseerd.TrimStart('+');
                if (nummerZonderPlus.Length > 15 || !Regex.IsMatch(nummerZonderPlus, @"^\d+$"))
                {
                    //throw new ArgumentException("Telefoonnummer voldoet niet aan de E.164-standaard.");
                    genormaliseerd = "";
                }
            }

            return genormaliseerd;
        }
    }
}
