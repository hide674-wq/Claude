#!/usr/bin/env python3
"""リファレンス画像の等身・画風を保ったまま、characters.json のキャラを Gemini で生成する。

使い方:
  export GEMINI_API_KEY=...            # https://aistudio.google.com/apikey
  python3 Art/gemini/generate.py                 # 全キャラ
  python3 Art/gemini/generate.py mage ninja      # id を指定
  python3 Art/gemini/generate.py --list

環境変数 GEMINI_MODEL で画像生成モデルを変更できる（既定: gemini-2.5-flash-image）。
標準ライブラリだけで動く。
"""
import argparse
import base64
import json
import os
import sys
import urllib.error
import urllib.request
from pathlib import Path

HERE = Path(__file__).resolve().parent
DEFAULT_MODEL = os.environ.get("GEMINI_MODEL", "gemini-2.5-flash-image")
ENDPOINT = "https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent"


def build_prompt(style, char):
    return (
        "The attached image is a style and proportion reference for a game character. "
        "Draw a NEW, different character in exactly the same art style and the same body proportions "
        "as the reference. Do not copy the reference character's costume, colours or weapon.\n\n"
        f"Proportions: {style['proportion']}.\n"
        f"Rendering: {style['rendering']}.\n"
        f"Composition: {style['composition']}.\n\n"
        f"New character: {char['description']}."
    )


def generate(api_key, model, prompt, ref_png):
    body = {
        "contents": [{
            "parts": [
                {"text": prompt},
                {"inline_data": {"mime_type": "image/png",
                                 "data": base64.b64encode(ref_png).decode()}},
            ]
        }],
        "generationConfig": {"responseModalities": ["TEXT", "IMAGE"]},
    }
    req = urllib.request.Request(
        ENDPOINT.format(model=model),
        data=json.dumps(body).encode(),
        headers={"Content-Type": "application/json", "x-goog-api-key": api_key},
    )
    try:
        with urllib.request.urlopen(req, timeout=180) as res:
            data = json.load(res)
    except urllib.error.HTTPError as e:
        raise RuntimeError(f"HTTP {e.code}: {e.read().decode(errors='replace')[:500]}") from None

    for cand in data.get("candidates", []):
        for part in cand.get("content", {}).get("parts", []):
            inline = part.get("inlineData") or part.get("inline_data")
            if inline:
                return base64.b64decode(inline["data"])
    raise RuntimeError("画像が返りませんでした: " + json.dumps(data, ensure_ascii=False)[:500])


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("ids", nargs="*", help="生成するキャラ id（省略で全員）")
    ap.add_argument("--list", action="store_true", help="キャラ一覧を表示して終了")
    ap.add_argument("--ref", default=str(HERE / "reference" / "hero_reference.png"))
    ap.add_argument("--out", default=str(HERE / "output"))
    ap.add_argument("--model", default=DEFAULT_MODEL)
    ap.add_argument("--print-prompt", action="store_true", help="API を呼ばずプロンプトだけ表示")
    args = ap.parse_args()

    spec = json.loads((HERE / "characters.json").read_text(encoding="utf-8"))
    chars = {c["id"]: c for c in spec["characters"]}

    if args.list:
        for c in spec["characters"]:
            print(f"{c['id']:15s} {c['name']}")
        return 0

    targets = args.ids or list(chars)
    unknown = [i for i in targets if i not in chars]
    if unknown:
        sys.exit(f"不明な id: {', '.join(unknown)}（--list で確認）")

    if args.print_prompt:
        for i in targets:
            print(f"--- {i} ---\n{build_prompt(spec['style'], chars[i])}\n")
        return 0

    api_key = os.environ.get("GEMINI_API_KEY")
    if not api_key:
        sys.exit("GEMINI_API_KEY が未設定です。")

    ref_png = Path(args.ref).read_bytes()
    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)

    failed = 0
    for i in targets:
        try:
            png = generate(api_key, args.model, build_prompt(spec["style"], chars[i]), ref_png)
        except Exception as e:  # noqa: BLE001 - 1 体の失敗で全体を止めない
            failed += 1
            print(f"[NG] {i}: {e}", file=sys.stderr)
            continue
        path = out / f"{i}.png"
        path.write_bytes(png)
        print(f"[OK] {chars[i]['name']} -> {path}")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
