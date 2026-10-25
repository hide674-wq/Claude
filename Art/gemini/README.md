# 同じ等身でキャラを量産する（Gemini）

`reference/hero_reference.png`（白フードの剣士）の **等身・画風** を保ったまま、別キャラを Gemini で生成するためのセット。

## 等身・画風の決まり（`characters.json` の `style`）

* 約 2.5 頭身。フード／頭が全身の約 4 割、脚は短く太い、手袋と靴は大きめ、武器は胴と同じくらい長い
* 濃い茶色の均一な輪郭線、フラットなセル塗り＋控えめなグラデのハイライト、面取りされた簡略形状
* くすんだ暖色系＋差し色 1 色、金具は淡い金／タン
* 1 体・全身・やや斜め前向き・無地のオフホワイト背景（#EFEBE4）・3:4 縦長・文字や枠なし

## 方法 A: Gemini アプリ／AI Studio で手作業

1. `reference/hero_reference.png` を添付
2. `python3 Art/gemini/generate.py --print-prompt mage` で出たプロンプトを貼り付ける
3. 気に入らなければ「リファレンスよりも頭を大きく／脚をもっと短く」のように等身を指示して再生成

## 方法 B: スクリプトで一括生成

```sh
export GEMINI_API_KEY=...        # https://aistudio.google.com/apikey
python3 Art/gemini/generate.py --list
python3 Art/gemini/generate.py             # 全キャラ → Art/gemini/output/<id>.png
python3 Art/gemini/generate.py mage ninja  # 指定キャラだけ
```

モデルは `GEMINI_MODEL`（既定 `gemini-2.5-flash-image`）で変更できます。

## キャラの追加

`characters.json` の `characters` に `id` / `name` / `description`（英語推奨）を足すだけ。
**武器・フード（帽子）・差し色・光る目の色** を書くとリファレンスの雰囲気が出ます。

## 等身がズレるときのコツ

* `description` に体型を書かない（`style.proportion` が一括で効く）
* 鎧や装飾を盛りすぎると頭身が伸びる → 装飾は 2〜3 個まで
* 気に入った生成結果を新しい `--ref` にして使うと、そのシリーズで統一しやすい
