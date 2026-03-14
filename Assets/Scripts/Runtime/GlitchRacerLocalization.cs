namespace GlitchRacer
{
    public static class GlitchRacerLocalization
    {
        public static string NormalizeLanguage(string language)
        {
            return language == "ru" ? "ru" : "en";
        }

        public static string LanguageName(string language, string uiLanguage)
        {
            language = NormalizeLanguage(language);
            uiLanguage = NormalizeLanguage(uiLanguage);

            if (uiLanguage == "ru")
            {
                return language == "ru" ? "Русский" : "Английский";
            }

            return language == "ru" ? "Russian" : "English";
        }

        public static string ToggleState(bool enabled, string language)
        {
            return NormalizeLanguage(language) == "ru"
                ? (enabled ? "Вкл" : "Выкл")
                : (enabled ? "On" : "Off");
        }

        public static string ActiveGlitchLabel(GlitchRacerGame.GlitchType glitchType, string language)
        {
            bool ru = NormalizeLanguage(language) == "ru";
            return glitchType switch
            {
                GlitchRacerGame.GlitchType.InvertControls => ru ? "инверсия управления" : "controls inverted",
                GlitchRacerGame.GlitchType.StaticNoise => ru ? "шум сигнала" : "signal noise",
                GlitchRacerGame.GlitchType.DrunkVision => ru ? "плывущее зрение" : "vision drifting",
                GlitchRacerGame.GlitchType.DrugsTrip => ru ? "психоделический сбой" : "drugs trip",
                _ => ru ? "стабильно" : "stable"
            };
        }

        public static string Meters(int value, string language)
        {
            return NormalizeLanguage(language) == "ru"
                ? $"{value:N0} м"
                : $"{value:N0} m";
        }

        public static string GlitchTimer(float time, string label, string language)
        {
            return NormalizeLanguage(language) == "ru"
                ? $"СБОЙ {time:0.0}с | {label}"
                : $"GLITCH {time:0.0}s | {label}";
        }

        public static string CriticalInstability(string language) =>
            NormalizeLanguage(language) == "ru"
                ? "КРИТИЧЕСКАЯ НЕСТАБИЛЬНОСТЬ СИСТЕМЫ"
                : "CRITICAL SYSTEM INSTABILITY";

        public static string Left(string language) => NormalizeLanguage(language) == "ru" ? "ЛЕВО" : "LEFT";
        public static string Right(string language) => NormalizeLanguage(language) == "ru" ? "ПРАВО" : "RIGHT";

        public static string ControlHint(bool touch, string language)
        {
            bool ru = NormalizeLanguage(language) == "ru";
            if (touch)
            {
                return ru
                    ? "Нажимай на левую или правую часть экрана, чтобы менять полосу."
                    : "Tap left or right side of the screen to switch lanes.";
            }

            return ru
                ? "A/D или стрелки влево/вправо для смены полосы."
                : "A/D or Left/Right to switch lanes.";
        }

        public static string BrandTagline(string language) =>
            NormalizeLanguage(language) == "ru"
                ? "СЛОМАННАЯ БЕЗДНА ДАННЫХ // ВИРУСНЫЙ РАННЕР"
                : "BROKEN DATA ABYSS // VIRUS RUNNER";

        public static string GameTitle(string language) =>
            NormalizeLanguage(language) == "ru"
                ? "СБОЙНЫЙ ГОНЩИК"
                : "GLITCH RACER";

        public static string MainMenuDescription(string language) =>
            NormalizeLanguage(language) == "ru"
                ? "Ныряй в сломанную бездну данных, переживай системные сбои и превращай каждый забег в постоянные улучшения."
                : "Dive through a broken data abyss, survive system glitches, and convert each run into permanent upgrades.";

        public static string WalletLabel(string language) => NormalizeLanguage(language) == "ru" ? "КОШЕЛЕК" : "WALLET";
        public static string WalletValue(int coins, string language) => NormalizeLanguage(language) == "ru" ? $"{coins:N0} монет" : $"{coins:N0} coins";
        public static string BestScore(string language) => NormalizeLanguage(language) == "ru" ? "ЛУЧШИЙ СЧЕТ" : "BEST SCORE";
        public static string BestDistance(string language) => NormalizeLanguage(language) == "ru" ? "ЛУЧШАЯ ДИСТАНЦИЯ" : "BEST DISTANCE";
        public static string TotalDistance(string language) => NormalizeLanguage(language) == "ru" ? "ОБЩАЯ ДИСТАНЦИЯ" : "TOTAL DISTANCE";
        public static string StartRun(string language) => NormalizeLanguage(language) == "ru" ? "Начать заезд" : "Start Run";
        public static string ShopUpgrades(string language) => NormalizeLanguage(language) == "ru" ? "Магазин / Улучшения" : "Shop / Upgrades";
        public static string Settings(string language) => NormalizeLanguage(language) == "ru" ? "Настройки" : "Settings";
        public static string RunPayout(string language) => NormalizeLanguage(language) == "ru" ? "НАГРАДА ЗА ЗАЕЗД" : "RUN PAYOUT";
        public static string PayoutFormula(string language) => NormalizeLanguage(language) == "ru" ? "3.5 x осколки + 0.03 x счет + 0.12 x метры" : "3.5 x shards + 0.03 x score + 0.12 x meters";
        public static string ShopTitle(string language) => NormalizeLanguage(language) == "ru" ? "Магазин" : "Shop";
        public static string FuelUpgradeTitle(int level, string language) => NormalizeLanguage(language) == "ru" ? $"Экономия RAM ур.{level}" : $"Fuel Efficiency Lv.{level}";
        public static string FuelUpgradeBody(float multiplier, string language) => NormalizeLanguage(language) == "ru" ? $"Снижает расход RAM на 8% за уровень.\nТекущий множитель расхода: x{multiplier:0.00}" : $"Reduces RAM drain by 8% per level.\nCurrent drain multiplier: x{multiplier:0.00}";
        public static string ScoreUpgradeTitle(int level, string language) => NormalizeLanguage(language) == "ru" ? $"Усилитель счета ур.{level}" : $"Score Booster Lv.{level}";
        public static string ScoreUpgradeBody(float multiplier, string language) => NormalizeLanguage(language) == "ru" ? $"Увеличивает весь получаемый счет на 12% за уровень.\nТекущий множитель счета: x{multiplier:0.00}" : $"Boosts all score gains by 12% per level.\nCurrent score multiplier: x{multiplier:0.00}";
        public static string Buy(int cost, string language) => NormalizeLanguage(language) == "ru" ? $"Купить {cost}" : $"Buy {cost}";
        public static string Back(string language) => NormalizeLanguage(language) == "ru" ? "Назад" : "Back";
        public static string MusicButton(bool enabled, string language) => NormalizeLanguage(language) == "ru" ? $"Музыка: {ToggleState(enabled, language)}" : $"Music: {ToggleState(enabled, language)}";
        public static string SfxButton(bool enabled, string language) => NormalizeLanguage(language) == "ru" ? $"Звуки: {ToggleState(enabled, language)}" : $"SFX: {ToggleState(enabled, language)}";
        public static string LanguageButton(string currentLanguage, string uiLanguage) => NormalizeLanguage(uiLanguage) == "ru" ? $"Язык: {LanguageName(currentLanguage, uiLanguage)}" : $"Language: {LanguageName(currentLanguage, uiLanguage)}";
        public static string ProgressSaved(string language) => NormalizeLanguage(language) == "ru" ? "Прогресс автоматически сохраняется после каждого завершенного заезда и покупки." : "Progress is saved automatically on every run end and purchase.";
        public static string SystemFailure(string language) => NormalizeLanguage(language) == "ru" ? "Системный Сбой" : "System Failure";
        public static string ScoreLine(float value, string language) => NormalizeLanguage(language) == "ru" ? $"Счет: {value:N0}" : $"Score: {value:N0}";
        public static string DistanceLine(float value, string language) => NormalizeLanguage(language) == "ru" ? $"Дистанция: {value:N0} м" : $"Distance: {value:N0} m";
        public static string DataShardsLine(int value, string language) => NormalizeLanguage(language) == "ru" ? $"Осколки данных: {value:N0}" : $"Data shards: {value:N0}";
        public static string CoinsEarnedLine(int value, string language) => NormalizeLanguage(language) == "ru" ? $"Получено монет: +{value:N0}" : $"Coins earned: +{value:N0}";
        public static string LeaderboardMetric(string language) => NormalizeLanguage(language) == "ru" ? "Метрика для таблицы лидеров: дистанция заезда в метрах. Используй это значение при отправке результата в лидерборды Яндекса." : "Leaderboard metric: run distance in meters. Use this when sending results to Yandex leaderboards.";
        public static string ReviveWatchAd(string language) => NormalizeLanguage(language) == "ru" ? "Возродиться (за рекламу)" : "Revive (Watch Ad)";
        public static string RunAgain(string language) => NormalizeLanguage(language) == "ru" ? "Снова в заезд" : "Run Again";
        public static string MainMenu(string language) => NormalizeLanguage(language) == "ru" ? "Главное меню" : "Main Menu";
        public static string ScoreLabel(string language) => NormalizeLanguage(language) == "ru" ? "СЧЕТ" : "SCORE";
        public static string DistanceLabel(string language) => NormalizeLanguage(language) == "ru" ? "ДИСТАНЦИЯ" : "DISTANCE";
        public static string RamStability(string language) => NormalizeLanguage(language) == "ru" ? "СТАБИЛЬНОСТЬ RAM" : "RAM STABILITY";
    }
}
