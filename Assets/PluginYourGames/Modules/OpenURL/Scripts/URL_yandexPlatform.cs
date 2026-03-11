#if YandexGamesPlatform_yg
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using YG.Utils.OpenURL;

namespace YG
{
    public partial class PlatformYG2 : IPlatformsYG2
    {
        class JsonAllGames
        {
            public int[] appID;
            public string[] title;
            public string[] url;
            public string[] coverURL;
            public string[] iconURL;
            public string developerURL;
        }

        [DllImport("__Internal")]
        private static extern string GetAllGames_js();

        public void GetAllGamesInit()
        {
            string jsonAllGamesStr = GetAllGames_js();
            if (jsonAllGamesStr == InfoYG.NO_DATA)
                return;

            JsonAllGames jsonAllGames = JsonUtility.FromJson<JsonAllGames>(jsonAllGamesStr);

            YG2.allGames = new GameInfo[jsonAllGames.appID.Length];

            for (int i = 0; i < jsonAllGames.appID.Length; i++)
            {
                YG2.allGames[i] = new GameInfo();
                YG2.allGames[i].appID = jsonAllGames.appID[i];
                YG2.allGames[i].title = jsonAllGames.title[i];
                YG2.allGames[i].url = jsonAllGames.url[i];
                YG2.allGames[i].coverURL = jsonAllGames.coverURL[i];
                YG2.allGames[i].iconURL = jsonAllGames.iconURL[i];
            }
            YG2.developerURL = jsonAllGames.developerURL;
        }


        public void OnURLDefineDomain(string url)
        {
            string domain = TryGetYandexDomain();
            url = "https://yandex." + domain + "/games/" + url;

            if (!string.IsNullOrEmpty(domain))
            {
                YG2.OnURL(url);
            }
            else
            {
#if RU_YG2
                YG2.Message("OnURL_Yandex_DefineDomain: Домен не определен!");
#else
                YG2.Message("OnURL_Yandex_DefineDomain: Domain not defined!");
#endif
            }
        }

        private static string TryGetYandexDomain()
        {
            FieldInfo envirField = typeof(YG2).GetField("envir", BindingFlags.Public | BindingFlags.Static);
            if (envirField == null)
                return "ru";

            object envirValue = envirField.GetValue(null);
            if (envirValue == null)
                return "ru";

            FieldInfo domainField = envirValue.GetType().GetField("domain", BindingFlags.Public | BindingFlags.Instance);
            if (domainField != null)
            {
                string fieldValue = domainField.GetValue(envirValue) as string;
                if (!string.IsNullOrEmpty(fieldValue))
                    return fieldValue;
            }

            PropertyInfo domainProperty = envirValue.GetType().GetProperty("domain", BindingFlags.Public | BindingFlags.Instance);
            if (domainProperty != null)
            {
                string propertyValue = domainProperty.GetValue(envirValue) as string;
                if (!string.IsNullOrEmpty(propertyValue))
                    return propertyValue;
            }

            return "ru";
        }
    }
}
#endif
