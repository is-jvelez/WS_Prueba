using System;
using System.Collections.Generic;
using System.Globalization;
using Legacy.Services.IS_WS_PRUEBA.Contracts;

namespace Legacy.Services.IS_WS_PRUEBA.Domain.Services
{
    public static class CampoParser
    {
        private static readonly string[] AllowedDateFormats =
        {
            "yyyy-MM-dd",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ssZ"
        };

        public static bool TryBuildFieldMap(SoapRequestDto request, out Dictionary<string, SoapCampoDto> map, out string error)
        {
            map = null;
            error = null;

            if (request == null)
            {
                error = "Request is required.";
                return false;
            }

            if (request.ListaCampos == null || request.ListaCampos.Count == 0)
            {
                error = "listaCampos is required.";
                return false;
            }

            map = new Dictionary<string, SoapCampoDto>(StringComparer.OrdinalIgnoreCase);

            for (var index = 0; index < request.ListaCampos.Count; index++)
            {
                var campo = request.ListaCampos[index];
                if (campo == null)
                {
                    error = "listaCampos contains null entries.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(campo.Name))
                {
                    error = "Each campo requires name.";
                    return false;
                }

                var normalizedName = campo.Name.Trim();
                if (map.ContainsKey(normalizedName))
                {
                    error = "Duplicate campo name: " + normalizedName + ".";
                    return false;
                }

                map[normalizedName] = campo;
            }

            return true;
        }

        public static bool TryGetRequiredString(Dictionary<string, SoapCampoDto> map, string name, out string value, out string error)
        {
            value = null;
            error = null;

            SoapCampoDto campo;
            if (!map.TryGetValue(name, out campo))
            {
                error = "Missing required campo: " + name + ".";
                return false;
            }

            if (!ValidateType(campo, "string", out error))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(campo.Value))
            {
                error = "Campo " + name + " cannot be empty.";
                return false;
            }

            value = campo.Value.Trim();
            return true;
        }

        public static bool TryGetRequiredInt(Dictionary<string, SoapCampoDto> map, string name, out int value, out string error)
        {
            value = 0;
            error = null;

            SoapCampoDto campo;
            if (!map.TryGetValue(name, out campo))
            {
                error = "Missing required campo: " + name + ".";
                return false;
            }

            if (!ValidateType(campo, "int", out error))
            {
                return false;
            }

            if (!int.TryParse(campo.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                error = "Campo " + name + " has invalid int value.";
                return false;
            }

            return true;
        }

        public static bool TryGetRequiredDate(Dictionary<string, SoapCampoDto> map, string name, out DateTime value, out string error)
        {
            value = DateTime.MinValue;
            error = null;

            SoapCampoDto campo;
            if (!map.TryGetValue(name, out campo))
            {
                error = "Missing required campo: " + name + ".";
                return false;
            }

            if (!ValidateType(campo, "datetime", out error))
            {
                return false;
            }

            if (!TryParseDate(campo.Value, out value))
            {
                error = "Campo " + name + " must use format yyyy-MM-dd.";
                return false;
            }

            return true;
        }

        public static bool TryGetOptionalString(Dictionary<string, SoapCampoDto> map, string name, out bool hasValue, out string value, out string error)
        {
            hasValue = false;
            value = null;
            error = null;

            SoapCampoDto campo;
            if (!map.TryGetValue(name, out campo))
            {
                return true;
            }

            if (!ValidateType(campo, "string", out error))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(campo.Value))
            {
                error = "Campo " + name + " cannot be empty when provided.";
                return false;
            }

            hasValue = true;
            value = campo.Value.Trim();
            return true;
        }

        public static bool TryGetOptionalDate(Dictionary<string, SoapCampoDto> map, string name, out bool hasValue, out DateTime value, out string error)
        {
            hasValue = false;
            value = DateTime.MinValue;
            error = null;

            SoapCampoDto campo;
            if (!map.TryGetValue(name, out campo))
            {
                return true;
            }

            if (!ValidateType(campo, "datetime", out error))
            {
                return false;
            }

            if (!TryParseDate(campo.Value, out value))
            {
                error = "Campo " + name + " must use format yyyy-MM-dd.";
                return false;
            }

            hasValue = true;
            return true;
        }

        private static bool ValidateType(SoapCampoDto campo, string expectedType, out string error)
        {
            error = null;

            if (campo == null)
            {
                error = "Campo is null.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(campo.Type))
            {
                error = "Campo " + campo.Name + " requires type.";
                return false;
            }

            if (!string.Equals(campo.Type.Trim(), expectedType, StringComparison.OrdinalIgnoreCase))
            {
                error = "Campo " + campo.Name + " expects type " + expectedType + ".";
                return false;
            }

            return true;
        }

        private static bool TryParseDate(string input, out DateTime value)
        {
            return DateTime.TryParseExact(
                input,
                AllowedDateFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out value);
        }
    }
}
