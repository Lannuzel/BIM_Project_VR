#!/usr/bin/env python3
"""Vérification de l'intégrité des fichiers d'une session (N2).

Usage :
    python check_session.py <dossier Data du casque> [--role Lecteur]

Le dossier attendu est celui copié depuis le casque :
    /sdcard/Android/data/<package>/files/Data
(adb pull "/sdcard/Android/data/<package>/files/Data" ./Data_Lecteur)

Pour chaque fichier produit par l'application :
    *_Audio.wav              voix (PCM 16 bits mono 44,1 kHz, marqueur sonore au lancement)
    *_EyeTrackingData.csv    regard (Time;RayOrigin;HitPoint;ObjectHit + lignes MARKER, END)
    *_FaceTrackingData.csv   expressions faciales (Timestamp;<unités>... + MARKER, END)
    *_UsersPositions.csv     positions (Timestamp;<user>_PosX... + MARKER, END)
le script contrôle : présence, taille, en-tête, présence du marqueur de lancement, fin « END »,
durée et fréquence moyenne, et signale les trous > 1 s.
Sortie : rapport texte ; code retour 1 si un contrôle échoue.
"""
import argparse
import glob
import os
import struct
import sys


def human(n):
    for unit in ("o", "Ko", "Mo", "Go"):
        if n < 1024:
            return f"{n:.1f} {unit}"
        n /= 1024
    return f"{n:.1f} To"


def check_wav(path, problems):
    size = os.path.getsize(path)
    print(f"  {os.path.basename(path)}  {human(size)}")
    if size < 44:
        problems.append(f"{path}: fichier WAV trop petit ({size} octets)")
        return
    with open(path, "rb") as f:
        header = f.read(44)
    if header[:4] != b"RIFF" or header[8:12] != b"WAVE":
        problems.append(f"{path}: en-tête WAV invalide (fichier non finalisé ?)")
        return
    channels, rate, bits = struct.unpack("<H", header[22:24])[0], struct.unpack("<I", header[24:28])[0], struct.unpack("<H", header[34:36])[0]
    data_size = struct.unpack("<I", header[40:44])[0]
    expected = size - 44
    if abs(data_size - expected) > 2:
        problems.append(f"{path}: taille de données annoncée {data_size} != réelle {expected} (arrêt non propre ?)")
    seconds = expected / max(1, rate * channels * bits // 8)
    print(f"      {channels} canal, {rate} Hz, {bits} bits, durée {seconds / 60:.1f} min")
    if seconds < 5:
        problems.append(f"{path}: audio très court ({seconds:.1f} s)")


def check_csv(path, kind, problems, expected_hz=None):
    size = os.path.getsize(path)
    print(f"  {os.path.basename(path)}  {human(size)}")
    with open(path, encoding="utf-8", errors="replace") as f:
        lines = f.read().split("\n")
    if not lines or not lines[0].strip():
        problems.append(f"{path}: fichier vide")
        return
    header = lines[0].strip()
    markers = [i for i, l in enumerate(lines) if ";MARKER" in l]
    has_end = any(l.strip().startswith("END") for l in lines[-3:])
    times = []
    for l in lines[1:]:
        if not l or ";MARKER" in l or l.startswith("END"):
            continue
        try:
            times.append(float(l.split(";")[0].replace(",", ".")))
        except ValueError:
            pass
    n = len(times)
    duration = (times[-1] - times[0]) if n > 1 else 0.0
    hz = (n - 1) / duration if duration > 0 else 0.0
    print(f"      {n} échantillons, durée {duration / 60:.1f} min, {hz:.1f} Hz moyen, "
          f"{len(markers)} marqueur(s), END={'oui' if has_end else 'non'}")
    print(f"      colonnes : {header[:90]}{'...' if len(header) > 90 else ''}")
    if not markers:
        problems.append(f"{path}: aucun marqueur de lancement (MARKER)")
    if not has_end:
        problems.append(f"{path}: pas de ligne END (arrêt non propre ?)")
    if n < 10:
        problems.append(f"{path}: trop peu d'échantillons ({n})")
    if expected_hz and hz > 0 and hz < expected_hz * 0.5:
        problems.append(f"{path}: fréquence moyenne {hz:.1f} Hz bien inférieure à l'attendu ({expected_hz} Hz)")
    gaps = [(times[i], times[i + 1] - times[i]) for i in range(n - 1) if times[i + 1] - times[i] > 1.0]
    if gaps:
        worst = max(gaps, key=lambda g: g[1])
        problems.append(f"{path}: {len(gaps)} trou(s) > 1 s (le pire : {worst[1]:.1f} s à t={worst[0]:.0f})")
    if kind == "eye" and n > 0:
        no_hit = sum(1 for l in lines[1:] if l.count(";") >= 7 and l.split(";")[7].strip() == "")
        if no_hit:
            print(f"      lignes sans objet regardé : {no_hit}")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("folder")
    ap.add_argument("--role", default="")
    ap.add_argument("--eye-hz", type=float, default=72.0, help="fréquence attendue du regard et du visage")
    ap.add_argument("--pos-hz", type=float, default=2.0, help="fréquence attendue des positions")
    args = ap.parse_args()

    problems = []
    print(f"Session : {args.folder}  {args.role}")
    kinds = [
        ("*_Audio.wav", "audio", None),
        ("*_EyeTrackingData.csv", "eye", args.eye_hz),
        ("*_FaceTrackingData.csv", "face", args.eye_hz),
        ("*_UsersPositions.csv", "pos", args.pos_hz),
    ]
    for pattern, kind, hz in kinds:
        files = sorted(glob.glob(os.path.join(args.folder, pattern)))
        print(f"\n[{kind}] {len(files)} fichier(s)")
        if not files:
            problems.append(f"aucun fichier {pattern}")
            continue
        for path in files:
            if kind == "audio":
                check_wav(path, problems)
            else:
                check_csv(path, kind, problems, hz)

    print("\n=== Résultat ===")
    if problems:
        for p in problems:
            print("PROBLÈME :", p)
        sys.exit(1)
    print("OK : tous les contrôles sont passés.")


if __name__ == "__main__":
    main()
