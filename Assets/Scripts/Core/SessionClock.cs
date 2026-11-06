using System;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Horloges communes aux enregistrements. Chaque ligne de CSV porte trois temps :
/// <c>Time</c> (secondes depuis le lancement de l'application sur ce casque, horloge locale),
/// <c>NetTime</c> (temps de simulation Photon Fusion, commun aux trois casques d'une session, -1 hors session)
/// et <c>Utc</c> (heure UTC du casque, ISO 8601 à la milliseconde).
/// L'alignement entre casques se fait sur <c>NetTime</c> ; <c>Utc</c> sert de secours si la session réseau tombe.
/// </summary>
public static class SessionClock
{
    /// <summary>En-tête des trois colonnes de temps, à placer en tête de chaque CSV.</summary>
    public const string HeaderColumns = "Time;NetTime;Utc";

    /// <summary>Temps de simulation Fusion (s), identique sur tous les casques connectés ; -1 si aucune session.</summary>
    public static double NetTime
    {
        get
        {
            try
            {
                return NetworkObjectUtils.IsRunning ? NetworkObjectUtils.Runner.SimulationTime : -1.0;
            }
            catch (Exception)
            {
                return -1.0;
            }
        }
    }

    /// <summary>Heure UTC ISO 8601 à la milliseconde (ex. 2026-11-06T13:05:12.345Z).</summary>
    public static string UtcIso => DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);

    /// <summary>Les trois valeurs de temps séparées par « ; », dans l'ordre de <see cref="HeaderColumns"/>.</summary>
    public static string Stamp()
    {
        return Time.time.ToString(CultureInfo.InvariantCulture) + ";" +
               NetTime.ToString("F3", CultureInfo.InvariantCulture) + ";" + UtcIso;
    }
}
