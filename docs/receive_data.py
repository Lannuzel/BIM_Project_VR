#!/usr/bin/env python3
"""Récepteur des données de session envoyées par les casques en fin de session (DataUploader).

Usage, sur le PC de l'expérimentateur, relié au même réseau que les casques :
    python docs/receive_data.py [--port 8765] [--out ./sessions]

Puis, dans Unity, renseigner l'URL dans Assets/Resources/SessionConfig.asset (champ uploadUrl) :
    http://<adresse IP du PC>:8765/upload
(l'adresse IP s'obtient avec `ipconfig` ; le pare-feu Windows doit autoriser Python sur ce port).

Chaque fichier reçu est rangé dans <out>/<date du marqueur>_<session>/<rôle>_<casque>/<nom du fichier>.
Le script ne remplace jamais un fichier existant : un suffixe numérique est ajouté.
"""
import argparse
import email
import email.policy
import os
import re
import sys
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer


def safe(name, default="inconnu"):
    name = re.sub(r"[^A-Za-z0-9._ -]+", "_", (name or "").strip())
    return name or default


class Handler(BaseHTTPRequestHandler):
    out_dir = "."

    def log_message(self, fmt, *args):  # journal plus lisible
        sys.stdout.write("%s - %s\n" % (self.address_string(), fmt % args))

    def do_GET(self):
        self.send_response(200)
        self.send_header("Content-Type", "text/plain; charset=utf-8")
        self.end_headers()
        self.wfile.write("Récepteur BIM VR prêt. Envoyer les fichiers en POST multipart sur /upload.\n".encode("utf-8"))

    def do_POST(self):
        length = int(self.headers.get("Content-Length", "0"))
        ctype = self.headers.get("Content-Type", "")
        body = self.rfile.read(length)
        if not ctype.startswith("multipart/form-data"):
            self.send_error(400, "multipart/form-data attendu")
            return

        msg = email.message_from_bytes(
            b"Content-Type: " + ctype.encode("utf-8") + b"\r\nMIME-Version: 1.0\r\n\r\n" + body,
            policy=email.policy.HTTP)
        fields, files = {}, []
        for part in msg.iter_parts():
            name = part.get_param("name", header="content-disposition")
            filename = part.get_filename()
            payload = part.get_payload(decode=True) or b""
            if filename:
                files.append((filename, payload))
            elif name:
                fields[name] = payload.decode("utf-8", "replace")

        session = safe(fields.get("session"), "session")
        role = safe(fields.get("role"), "role")
        device = safe(fields.get("device"), "casque")
        marker = safe(fields.get("markerUtc"), "")[:19].replace(":", "-")
        folder = os.path.join(self.out_dir, f"{marker}_{session}" if marker else session, f"{role}_{device}")
        os.makedirs(folder, exist_ok=True)

        saved = []
        for filename, payload in files:
            target = os.path.join(folder, safe(os.path.basename(filename), "fichier"))
            base, ext = os.path.splitext(target)
            n = 1
            while os.path.exists(target):
                target = f"{base}_{n}{ext}"
                n += 1
            with open(target, "wb") as fh:
                fh.write(payload)
            saved.append(target)
            print(f"  reçu {len(payload) / 1048576:.2f} Mo -> {target}")

        self.send_response(200)
        self.send_header("Content-Type", "text/plain; charset=utf-8")
        self.end_headers()
        self.wfile.write(("OK %d fichier(s)\n" % len(saved)).encode("utf-8"))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--port", type=int, default=8765)
    ap.add_argument("--out", default="./sessions")
    args = ap.parse_args()
    Handler.out_dir = os.path.abspath(args.out)
    os.makedirs(Handler.out_dir, exist_ok=True)
    server = ThreadingHTTPServer(("0.0.0.0", args.port), Handler)
    print(f"Récepteur en écoute sur le port {args.port}, fichiers dans {Handler.out_dir} (Ctrl+C pour arrêter)")
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass


if __name__ == "__main__":
    main()
