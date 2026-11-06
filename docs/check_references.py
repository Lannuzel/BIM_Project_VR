"""Références GUID vers des assets absents du projet (scènes, prefabs, matériaux, ProjectSettings).

Usage :
    python docs/check_references.py <dossier du projet> [--restore] [--from <commit>]

Pour chaque GUID référencé par un fichier YAML d'Assets ou de ProjectSettings et absent des .meta
d'Assets, Packages et Library/PackageCache, le script indique les fichiers qui le référencent et,
si le GUID existait dans <commit> (défaut : a85a677^, état juste avant le nettoyage du 6 novembre
2026), le chemin d'origine. --restore restaure ces fichiers (et leur .meta) depuis <commit>.
À relancer jusqu'à ce qu'il ne reste plus de ligne MANQUANT (les fichiers restaurés peuvent en
référencer d'autres). Un asset manquant s'affiche en magenta sur casque sans erreur de build.
"""
import os, re, subprocess, sys, collections

proj = sys.argv[1]
restore = '--restore' in sys.argv
base = sys.argv[sys.argv.index('--from') + 1] if '--from' in sys.argv else 'a85a677^'
os.chdir(proj)
YAML_EXT = {'.unity', '.prefab', '.mat', '.asset', '.controller', '.anim', '.overrideController', '.mask',
            '.physicMaterial', '.renderTexture', '.flare', '.guiskin', '.fontsettings', '.terrainlayer',
            '.spriteatlas', '.preset', '.signal', '.playable', '.cubemap', '.fusion', '.shadervariants',
            '.lighting', '.giparams', '.mixer', '.brush', '.shadergraph', '.shadersubgraph'}
guid_re = re.compile(rb'guid: ([0-9a-f]{32})')

existing = set()
for folder in ('Assets', 'Library/PackageCache', 'Packages'):
    for root, _, files in os.walk(folder):
        for f in files:
            if f.endswith('.meta'):
                try:
                    with open(os.path.join(root, f), 'rb') as fh:
                        head = fh.read(300)
                except OSError:
                    continue
                m = guid_re.search(head)
                if m:
                    existing.add(m.group(1).decode())
print('GUIDs existants :', len(existing))

refs = collections.defaultdict(set)
for root, _, files in os.walk('Assets'):
    for f in files:
        ext = os.path.splitext(f)[1]
        if ext not in YAML_EXT:
            continue
        p = os.path.join(root, f)
        try:
            data = open(p, 'rb').read()
        except OSError:
            continue
        for m in guid_re.finditer(data):
            g = m.group(1).decode()
            if g.startswith('0000000000000000') or g in existing:
                continue
            refs[g].add(p.replace('\\', '/'))
for f in ('ProjectSettings/GraphicsSettings.asset', 'ProjectSettings/QualitySettings.asset', 'ProjectSettings/ProjectSettings.asset'):
    data = open(f, 'rb').read()
    for m in guid_re.finditer(data):
        g = m.group(1).decode()
        if not g.startswith('0000000000000000') and g not in existing:
            refs[g].add(f)

# chemins d'origine dans le commit de référence
old = {}
out = subprocess.run(['git', 'grep', '-E', '^guid: ', base, '--', '*.meta'], capture_output=True).stdout.decode('utf-8', 'replace')
for line in out.splitlines():
    m = re.match(re.escape(base) + r':(.*)\.meta:guid: ([0-9a-f]{32})', line)
    if m:
        old[m.group(2)] = m.group(1)

print('GUIDs manquants référencés :', len(refs))
by_file = collections.defaultdict(list)
unknown = []
for g, files in sorted(refs.items(), key=lambda x: -len(x[1])):
    path = old.get(g)
    if path is None:
        unknown.append((g, sorted(files)))
        continue
    by_file[path].extend(sorted(files))
for path, files in sorted(by_file.items()):
    print(f'  MANQUANT {path}  <- {len(set(files))} fichier(s) : {", ".join(sorted(set(files))[:3])}')
print(f'Inconnus (jamais dans {base}) :', len(unknown))
for g, files in unknown[:15]:
    print(f'  ? {g} <- {files[:3]}')

if restore and by_file:
    paths = []
    for p in by_file:
        paths += [p, p + '.meta']
    subprocess.run(['git', 'checkout', base, '--'] + paths, check=True)
    print('Restaurés :', len(by_file))
