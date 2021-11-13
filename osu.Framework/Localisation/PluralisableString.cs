// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;

#nullable enable

namespace osu.Framework.Localisation
{
    /// <summary>
    /// A string which can display a plural variant of a localised string according to the plural context.
    /// </summary>
    public class PluralisableString : TranslatableString
    {
        public readonly int Count;

        public readonly char Separator;

        public PluralisableString(string key, string fallback, int count, char separator, params object[] args)
            : base(key, fallback, args)
        {
            Count = count;
            Separator = separator;
        }

        public PluralisableString(string key, FormattableString interpolation, int count, char separator)
            : base(key, interpolation)
        {
            Count = count;
            Separator = separator;
        }

        protected override string GetLocalisedFormat(LocalisationParameters parameters, string format)
        {
            string[] variants = format.Split(Separator);
            return variants.ElementAtOrDefault(getPluralIndex(parameters)) ?? variants.ElementAt(variants.Length);
        }

        public bool Equals(PluralisableString? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Count == other.Count && Equals(other as TranslatableString);
        }

        public override bool Equals(ILocalisableStringData? other) => other is PluralisableString pluralisable && Equals(pluralisable);
        public override bool Equals(object? obj) => obj is PluralisableString pluralisable && Equals(pluralisable);

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(base.GetHashCode());
            hashCode.Add(Count);
            return hashCode.ToHashCode();
        }

        #region Mapping of plural variant indices according to locale

        private int getPluralIndex(LocalisationParameters parameters)
        {
            switch (parameters.Store?.EffectiveCulture?.Name ?? string.Empty)
            {
                case "af":
                case "af_ZA":
                case "bn":
                case "bn_BD":
                case "bn_IN":
                case "bg":
                case "bg_BG":
                case "ca":
                case "ca_AD":
                case "ca_ES":
                case "ca_FR":
                case "ca_IT":
                case "da":
                case "da_DK":
                case "de":
                case "de_AT":
                case "de_BE":
                case "de_CH":
                case "de_DE":
                case "de_LI":
                case "de_LU":
                case "el":
                case "el_CY":
                case "el_GR":
                case "en":
                case "en_AG":
                case "en_AU":
                case "en_BW":
                case "en_CA":
                case "en_DK":
                case "en_GB":
                case "en_HK":
                case "en_IE":
                case "en_IN":
                case "en_NG":
                case "en_NZ":
                case "en_PH":
                case "en_SG":
                case "en_US":
                case "en_ZA":
                case "en_ZM":
                case "en_ZW":
                case "eo":
                case "eo_US":
                case "es":
                case "es_AR":
                case "es_BO":
                case "es_CL":
                case "es_CO":
                case "es_CR":
                case "es_CU":
                case "es_DO":
                case "es_EC":
                case "es_ES":
                case "es_GT":
                case "es_HN":
                case "es_MX":
                case "es_NI":
                case "es_PA":
                case "es_PE":
                case "es_PR":
                case "es_PY":
                case "es_SV":
                case "es_US":
                case "es_UY":
                case "es_VE":
                case "et":
                case "et_EE":
                case "eu":
                case "eu_ES":
                case "eu_FR":
                case "fa":
                case "fa_IR":
                case "fi":
                case "fi_FI":
                case "fo":
                case "fo_FO":
                case "fur":
                case "fur_IT":
                case "fy":
                case "fy_DE":
                case "fy_NL":
                case "gl":
                case "gl_ES":
                case "gu":
                case "gu_IN":
                case "ha":
                case "ha_NG":
                case "he":
                case "he_IL":
                case "hu":
                case "hu_HU":
                case "is":
                case "is_IS":
                case "it":
                case "it_CH":
                case "it_IT":
                case "ku":
                case "ku_TR":
                case "lb":
                case "lb_LU":
                case "ml":
                case "ml_IN":
                case "mn":
                case "mn_MN":
                case "mr":
                case "mr_IN":
                case "nah":
                case "nb":
                case "nb_NO":
                case "ne":
                case "ne_NP":
                case "nl":
                case "nl_AW":
                case "nl_BE":
                case "nl_NL":
                case "nn":
                case "nn_NO":
                case "no":
                case "om":
                case "om_ET":
                case "om_KE":
                case "or":
                case "or_IN":
                case "pa":
                case "pa_IN":
                case "pa_PK":
                case "pap":
                case "pap_AN":
                case "pap_AW":
                case "pap_CW":
                case "ps":
                case "ps_AF":
                case "pt":
                case "pt_BR":
                case "pt_PT":
                case "so":
                case "so_DJ":
                case "so_ET":
                case "so_KE":
                case "so_SO":
                case "sq":
                case "sq_AL":
                case "sq_MK":
                case "sv":
                case "sv_FI":
                case "sv_SE":
                case "sw":
                case "sw_KE":
                case "sw_TZ":
                case "ta":
                case "ta_IN":
                case "ta_LK":
                case "te":
                case "te_IN":
                case "tk":
                case "tk_TM":
                case "ur":
                case "ur_IN":
                case "ur_PK":
                case "zu":
                case "zu_ZA":
                    return (Count == 1) ? 0 : 1;

                case "am":
                case "am_ET":
                case "bh":
                case "fil":
                case "fil_PH":
                case "fr":
                case "fr_BE":
                case "fr_CA":
                case "fr_CH":
                case "fr_FR":
                case "fr_LU":
                case "gun":
                case "hi":
                case "hi_IN":
                case "hy":
                case "hy_AM":
                case "ln":
                case "ln_CD":
                case "mg":
                case "mg_MG":
                case "nso":
                case "nso_ZA":
                case "ti":
                case "ti_ER":
                case "ti_ET":
                case "wa":
                case "wa_BE":
                case "xbr":
                    return ((Count == 0) || (Count == 1)) ? 0 : 1;

                case "be":
                case "be_BY":
                case "bs":
                case "bs_BA":
                case "hr":
                case "hr_HR":
                case "ru":
                case "ru_RU":
                case "ru_UA":
                case "sr":
                case "sr_ME":
                case "sr_RS":
                case "uk":
                case "uk_UA":
                    return ((Count % 10 == 1) && (Count % 100 != 11)) ? 0 : (((Count % 10 >= 2) && (Count % 10 <= 4) && ((Count % 100 < 10) || (Count % 100 >= 20))) ? 1 : 2);

                case "cs":
                case "cs_CZ":
                case "sk":
                case "sk_SK":
                    return (Count == 1) ? 0 : (((Count >= 2) && (Count <= 4)) ? 1 : 2);

                case "ga":
                case "ga_IE":
                    return (Count == 1) ? 0 : ((Count == 2) ? 1 : 2);

                case "lt":
                case "lt_LT":
                    return ((Count % 10 == 1) && (Count % 100 != 11)) ? 0 : (((Count % 10 >= 2) && ((Count % 100 < 10) || (Count % 100 >= 20))) ? 1 : 2);

                case "sl":
                case "sl_SI":
                    return (Count % 100 == 1) ? 0 : ((Count % 100 == 2) ? 1 : (((Count % 100 == 3) || (Count % 100 == 4)) ? 2 : 3));

                case "mk":
                case "mk_MK":
                    return (Count % 10 == 1) ? 0 : 1;

                case "mt":
                case "mt_MT":
                    return (Count == 1) ? 0 : (((Count == 0) || ((Count % 100 > 1) && (Count % 100 < 11))) ? 1 : (((Count % 100 > 10) && (Count % 100 < 20)) ? 2 : 3));

                case "lv":
                case "lv_LV":
                    return (Count == 0) ? 0 : (((Count % 10 == 1) && (Count % 100 != 11)) ? 1 : 2);

                case "pl":
                case "pl_PL":
                    return (Count == 1) ? 0 : (((Count % 10 >= 2) && (Count % 10 <= 4) && ((Count % 100 < 12) || (Count % 100 > 14))) ? 1 : 2);

                case "cy":
                case "cy_GB":
                    return (Count == 1) ? 0 : ((Count == 2) ? 1 : (((Count == 8) || (Count == 11)) ? 2 : 3));

                case "ro":
                case "ro_RO":
                    return (Count == 1) ? 0 : (((Count == 0) || ((Count % 100 > 0) && (Count % 100 < 20))) ? 1 : 2);

                case "ar":
                case "ar_AE":
                case "ar_BH":
                case "ar_DZ":
                case "ar_EG":
                case "ar_IN":
                case "ar_IQ":
                case "ar_JO":
                case "ar_KW":
                case "ar_LB":
                case "ar_LY":
                case "ar_MA":
                case "ar_OM":
                case "ar_QA":
                case "ar_SA":
                case "ar_SD":
                case "ar_SS":
                case "ar_SY":
                case "ar_TN":
                case "ar_YE":
                    return (Count == 0) ? 0 : ((Count == 1) ? 1 : ((Count == 2) ? 2 : (((Count % 100 >= 3) && (Count % 100 <= 10)) ? 3 : (((Count % 100 >= 11) && (Count % 100 <= 99)) ? 4 : 5))));

                default:
                    return 0;
            }
        }

        #endregion
    }
}
