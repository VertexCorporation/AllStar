using UnityEngine;
using System;

namespace SgLib
{
    public static class Utilities
    {

        public static void ErrorSound()
        {
            SoundManager.Instance.PlaySound(SoundManager.Instance.erro);
        }

        public static void ButtonClickSound()
        {
            SoundManager.Instance.PlaySound(SoundManager.Instance.button);
        }

        /// <summary>
        /// Stores a DateTime as string to PlayerPrefs.
        /// </summary>
        /// <param name="time">Time.</param>
        /// <param name="ppkey">Ppkey.</param>
        public static void StoreTime(string ppkey, DateTime time)
        {
            PlayerPrefs.SetString(ppkey, time.ToBinary().ToString());
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Gets the stored string in the PlayerPrefs and converts it to a DateTime.
        /// If no time stored previously, defaultTime is returned.
        /// </summary>
        /// <returns>The time.</returns>
        /// <param name="ppkey">Ppkey.</param>
        public static DateTime GetTime(string ppkey, DateTime defaultTime)
        {
            string storedTime = PlayerPrefs.GetString(ppkey, string.Empty);

            if (!string.IsNullOrEmpty(storedTime))
                return DateTime.FromBinary(Convert.ToInt64(storedTime));
            else
                return defaultTime;
        }
    }
}