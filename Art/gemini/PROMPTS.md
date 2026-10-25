# Gemini チャット用プロンプト（無料枠）

`characters.json` から自動生成。gemini.google.com で「画像を作成」を選び、手順どおりに貼ります。

## 手順

1. 新しいチャットを開き、`reference/hero_reference.png` を添付する
2. 下の **【1回目】** を貼って送信（最初の1体で画風と等身を固定）
3. 同じチャットで **【2体目以降】** を1体ずつ貼る（チャットが画風を覚えているので短くて済む）
4. 等身がズレたら「リファレンス画像と同じ2.5頭身に。頭を大きく、脚を短く」と追加で指示する
5. 無料枠は1日の生成枚数に上限がある。上限に達したら翌日に続きを行う

## 【1回目】魔法使い（画風・等身の固定）

```text
The attached image is a style and proportion reference for a game character. Draw a NEW, different character in exactly the same art style and the same body proportions as the reference. Do not copy the reference character's costume, colours or weapon.

Proportions: chibi, about 2.5 heads tall: the oversized hood/head is roughly 40% of the total height, short thick legs, large gloved hands, big chunky boots, a weapon that is nearly as long as the torso.
Rendering: clean dark-brown outline of uniform weight, flat cel shading with a few soft gradient highlights, chunky simplified faceted shapes, muted warm palette with one strong accent colour, subtle metallic trim in tan/gold.
Composition: single character, full body, standing, slight three-quarter front view, weapon held low in front, plain off-white (#EFEBE4) background with a faint ground shadow, portrait 3:4 canvas, no text, no frame.

New character: a young mage in a tall pointed indigo hat with a gold star emblem, oversized sleeves, a short purple cape, round spectacles-like glowing cyan eyes in the shadow of the hat, holding a crooked wooden staff topped with a glowing crystal.
```

## 【2体目以降】

同じチャットで、キャラごとに以下を貼る。

### 弓使い（archer）

```text
添付画像と最初に作った魔法使いと同じ画風・同じ2.5頭身（フード/頭が全身の約4割、脚は短く太い、大きな手袋と靴、武器は胴と同じくらいの長さ）、同じ構図（全身1体、やや斜め前向き、無地オフホワイト背景、3:4縦長、文字なし）で、別キャラを描いてください。
新キャラ: a ranger with a green hood and feathered cap, a leaf-green scarf, leather vest and bracers, a quiver on the back, glowing amber eyes under the hood, holding a big recurve bow with an arrow nocked.
```

### 僧侶（cleric）

```text
添付画像と最初に作った魔法使いと同じ画風・同じ2.5頭身（フード/頭が全身の約4割、脚は短く太い、大きな手袋と靴、武器は胴と同じくらいの長さ）、同じ構図（全身1体、やや斜め前向き、無地オフホワイト背景、3:4縦長、文字なし）で、別キャラを描いてください。
新キャラ: a gentle healer in a white and gold mitre-like hood, long white robe with teal trim, a small round shield on the arm, soft glowing pink eyes, holding a heavy mace shaped like a golden sun.
```

### 忍者（ninja）

```text
添付画像と最初に作った魔法使いと同じ画風・同じ2.5頭身（フード/頭が全身の約4割、脚は短く太い、大きな手袋と靴、武器は胴と同じくらいの長さ）、同じ構図（全身1体、やや斜め前向き、無地オフホワイト背景、3:4縦長、文字なし）で、別キャラを描いてください。
新キャラ: a ninja in a dark navy hood with a metal forehead plate, a long red scarf flowing behind, light armour plates on shoulders and shins, glowing white eyes, holding a short kunai-like blade in a reverse grip.
```

### 竜騎士（dragon_knight）

```text
添付画像と最初に作った魔法使いと同じ画風・同じ2.5頭身（フード/頭が全身の約4割、脚は短く太い、大きな手袋と靴、武器は胴と同じくらいの長さ）、同じ構図（全身1体、やや斜め前向き、無地オフホワイト背景、3:4縦長、文字なし）で、別キャラを描いてください。
新キャラ: a heavy knight with a horned dragon-scale helmet in dark crimson and bronze, a big shoulder pauldron shaped like a dragon head, glowing orange eyes in the visor shadow, holding an oversized lance with a flag.
```

### 錬金術師（alchemist）

```text
添付画像と最初に作った魔法使いと同じ画風・同じ2.5頭身（フード/頭が全身の約4割、脚は短く太い、大きな手袋と靴、武器は胴と同じくらいの長さ）、同じ構図（全身1体、やや斜め前向き、無地オフホワイト背景、3:4縦長、文字なし）で、別キャラを描いてください。
新キャラ: an alchemist in a goggled brown hood, a stained apron with many pouches and glass vials on the belt, thick rubber gloves, glowing lime-green eyes, holding a bubbling flask in one hand and a wrench in the other.
```

### ロボ兵（robot_guard）

```text
添付画像と最初に作った魔法使いと同じ画風・同じ2.5頭身（フード/頭が全身の約4割、脚は短く太い、大きな手袋と靴、武器は胴と同じくらいの長さ）、同じ構図（全身1体、やや斜め前向き、無地オフホワイト背景、3:4縦長、文字なし）で、別キャラを描いてください。
新キャラ: a small steel guard robot with a dome helmet hood, a single glowing yellow visor slit, riveted plates, a tattered blue cloak, holding a big rectangular tower shield and a short sword.
```

### 海賊（pirate）

```text
添付画像と最初に作った魔法使いと同じ画風・同じ2.5頭身（フード/頭が全身の約4割、脚は短く太い、大きな手袋と靴、武器は胴と同じくらいの長さ）、同じ構図（全身1体、やや斜め前向き、無地オフホワイト背景、3:4縦長、文字なし）で、別キャラを描いてください。
新キャラ: a pirate in a big black tricorn hat with a skull emblem pulled low over a shadowed face, glowing red eyes, a teal coat with brass buttons, a sash, holding an oversized cutlass.
```
