# Slingshot Heroes（スリングショット ヒーローズ）

「スリングショットブレイブス」のような **引っ張って飛ばす爽快アクション RPG** の Android 向け Unity プロジェクトです。
キャラクター画像・効果音はすべてコードで生成しているので、外部アセット無しでそのまま動きます。
（名前・画像・数値は独自のもので、ゲームの「仕組み」だけを参考にしています）

## 遊び方

| 操作 | 内容 |
|---|---|
| 画面をドラッグして離す | 引っ張った **逆方向** にヒーローが飛び出す（引く長さ＝パワー）。点線が軌道予測（壁で反射）|
| 上部のヒーロー顔アイコン | 行動させるヒーローを切り替え |
| 「スキル」ボタン | ヒーローごとの特技を発動（クールダウン制）|
| リタイア | 戦闘を中断 |

* ヒーロー 3 人が 1 ターンに 1 回ずつ行動 → 敵ターン、の繰り返し（3 ウェーブ、最後はボス）。
* 敵に当てるたびに **COMBO** が増え、ダメージが上がる。吹っ飛んだ敵同士がぶつかると追加ダメージ。
* 飛ばしたヒーローが **味方にぶつかる** と「LINK」発動。その味方の周囲に連携攻撃（ティナなら回復も）。
* 敵の頭上の **●の数** が攻撃までのターン数。赤く点滅したら次の敵ターンで攻撃してくる。
  * ルビーン／オレンジ … 突進　イエロン／ダークスパイク … 弾を撃つ　魔獣ガルド … 範囲衝撃波＋弾幕
* スキル: アルス「渾身撃」(次の一撃 2.5 倍) / ミラ「爆裂」(敵全体にダメージ) / ティナ「ヒール」(味方全員回復)
* ステージは 3 つ。前のステージをクリアすると次が解放。獲得コインは端末に保存されます。

## 開発環境

* Unity **2022.3 LTS** 推奨（`ProjectSettings/ProjectVersion.txt` は 2022.3.20f1。別バージョンで開く場合はアップグレードを許可してください）
* Unity Hub で **Android Build Support**（OpenJDK / Android SDK & NDK 含む）を追加しておく

## 実行・ビルド

1. Unity Hub →「開く」でこのフォルダを選択。初回起動時に `Assets/Scenes/Main.unity` とビルド設定が自動で作られます
   （「Active Input Handling を Both に変更した」ログが出たらエディタを 1 回再起動）。
2. `Assets/Scenes/Main.unity` を開いて ▶ で再生（Game ビューは縦長の 9:16 / 9:20 推奨）。
3. Android ビルド: メニュー **Tools > Slingshot Heroes > Build Android APK** → `Builds/SlingshotHeroes.apk`
   （Google Play 用は *Build Android App Bundle (AAB)*）。
   コマンドラインなら
   `Unity -batchmode -quit -projectPath . -buildTarget Android -executeMethod SlingshotHeroes.EditorTools.BuildScript.BuildAndroid`
4. 端末に入れる: `adb install -r Builds/SlingshotHeroes.apk`

パッケージ名は `Assets/Editor/ProjectSetup.cs` の `AppId`（`com.hide674.slingshotheroes`）で変更できます。

## コード構成（`Assets/Scripts`）

| ファイル | 役割 |
|---|---|
| `Game.cs` | 起動処理（シーンに何も置かなくても自動生成）、タイトル／ステージ選択／リザルト画面、セーブ |
| `Battle.cs` | 戦闘本体：アリーナ、スリングショット操作、独自の円形物理、ターン進行、敵 AI、スキル、連携、コンボ |
| `Units.cs` | `Hero` / `Enemy` / `Bullet`（HP バー、被ダメ演出、攻撃カウントの●表示）|
| `Hud.cs` | 戦闘中 UI（ヒーローカード、スキルボタン、コンボ、ダメージ数字、バナー）|
| `Data.cs` | ヒーロー・敵・ステージのデータ（ここを編集してバランス調整）|
| `Art.cs` | キャラ・床・UI アイコンをコードでお絵描き（`Pix` が簡易ラスタライザ）|
| `Sfx.cs` / `Fx.cs` / `UIKit.cs` | 効果音の合成 / パーティクル / uGUI ヘルパー |

### カスタマイズのヒント

* ヒーロー・敵・ステージ追加: `Data.cs` の `GameData` に定義を足すだけ（敵の見た目は `EnemyKind` と色で自動生成）。
* 操作感: `Battle.cs` 先頭の `MaxPull`（引っ張り最大距離）、`MinSpeed`/`MaxSpeed`（発射速度）、`ShooterDamp`（減速）。
* 自作の絵や音に差し替える場合は `Art.cs` / `Sfx.cs` の生成部分を `Resources.Load` に置き換えれば OK。
* 日本語が豆腐（□）になる端末があれば、日本語フォントを取り込んで `UIKit.Font` に代入してください。
