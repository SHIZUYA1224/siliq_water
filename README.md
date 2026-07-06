# Siliq Water Maps Studio

Unity エディタ上(またはランタイム)で、水面・流体表現向けの **PBR テクスチャ一式を手続き生成**するツールです。

- **VRChat モバイル (Quest / Android / iOS)** — 生成物はただの PNG テクスチャなのでそのまま使用可
- **最新の Unity (ビルトイン / URP)** — フォーム・フロー・ラフネスマップまで含む本格的な水表現に対応。URP シェーダーは任意 Sample として導入
- **ランタイム API** — ビルドにテクスチャを含めず、ロード時に動的生成することも可能
- **触れたら波紋が広がるインタラクティブな水面** — アバターが水に入った位置から波紋が実時間で広がる (同梱シェーダー限定)

対応: Unity 2019.4 以降 / ビルトイン RP・URP (URP シェーダーは Package Manager Sample)

## 生成できるマップ (7 種類)

| マップ | 用途 |
|---|---|
| **ノーマルマップ** | 波の凹凸。あらゆる水シェーダーの基本 |
| **ハイトマップ** | 頂点ディスプレイスメント、視差、他ツール連携 |
| **フォームマスク** | 波頭・砕け波の白泡(しきい値・砕け波ブースト調整可) |
| **ラフネス / スムースネス** | 傾斜とフォームから物理的に妥当な粗さを合成 |
| **フローマップ** | RG = 流れベクトル。レイヤーの進行方向と渦成分を合成 |
| **DUDV マップ** | 屈折・ディストーション用オフセット |
| **コースティクス (近似)** | 光の収束(負のラプラシアン)から水底の光模様を生成 |

すべて同一のハイトフィールドから導出されるため、**マップ間の整合性が完全に取れています**(フォームの位置とノーマルの波頭が一致する等)。

## 特徴

- **必ずシームレスにタイリング** — 全ノイズをトーラス上で定義。継ぎ目は一切出ません
- **6 種類の波レイヤー** を自由に重ねる(加算 / 乗算 / 最大 / 最小)
  - 揺らぎノイズ (パーリン fBm) / 尖ったうねり (リッジ) / ボロノイ泡 / ボロノイ網目 / 指向性の波 (最大 64 波のスペクトル合成) / 雨の波紋
- **ドメインワープ** — 模様を有機的に歪ませるプロ品質のノイズ加工
- **マスクむら** — 低周波ノイズでレイヤーの効きに自然なムラを付与
- **完全ループするアニメーション書き出し** — 連番 / フリップブックアトラス。全レイヤー速度が整数設計のため最終フレームが先頭へ正確に繋がります
- **16bit EXR 書き出し** — 穏やかな水面で目立つ 8bit のバンディング (縞) を根絶
- **スーパーサンプリング (2×)** — 鋭いエッジのジャギーを抑えた滑らかな出力
- **プリセット 10 種** — 湖 / 海 / 外洋 (スペクトル) / 川 / 雨 / さざ波 / トゥーン / 溶岩 / プール / サイバー
- **プロファイル (ScriptableObject)** — 設定をアセットとして保存・共有。JSON コピー & ペーストにも対応
- **マテリアル自動作成** — 書き出したマップを Standard / URP Lit / 同梱水シェーダーへ割り当て済みのマテリアルを生成
- **モバイル向けインポート設定の自動適用** — Repeat / NormalMap タイプ / Android・iOS=ASTC 6x6

共通 Editor ツール UI の設計方針は [Docs/CommonEditorToolLayoutSpec.md](Docs/CommonEditorToolLayoutSpec.md) にまとめています。
用途別マテリアルプリセットの使い分けは [Docs/MaterialLookPresetGuide.md](Docs/MaterialLookPresetGuide.md) を参照してください。
水面マップスタジオでは `目的から始める` から、綺麗な海 / 透明プール / 血の海 / 液体金属の推奨生成設定を一括適用できます。
公開・リリース前の確認項目は [Docs/PublicReleaseChecklist.md](Docs/PublicReleaseChecklist.md) にまとめています。

## 公開リポジトリとしての状態

- `LICENSE.md`: 現時点では製品向けの All rights reserved。配布条件を変える場合は公開前に差し替えてください。
- `SECURITY.md`: 脆弱性や secret 露出の非公開報告ルール。
- `CONTRIBUTING.md`: 変更時の品質基準と検証手順。
- `.gitignore` / `.gitattributes`: Unity 生成物の混入防止と text/binary 管理。

## 焼き済みパック (PrebakedPack) — ツール不要ですぐ使える 5 種

ツールを触らなくても、`PrebakedPack/` に**すぐ使える水ノーマルマップ 5 種 + 設定済みマテリアル + サンプルシーン**が入っています。

| ファイル | 用途 | マテリアル |
|---|---|---|
| `Water_Normal_Calm_01.png` | 静かな湖・穏やかな水面 | `M_Water_Calm` |
| `Water_Normal_Ripple_01.png` | 雨の波紋 | `M_Water_Ripple` |
| `Water_Normal_Stream_01.png` | 川・一方向に流れる水 | `M_Water_Stream` |
| `Water_Normal_Pool_01.png` | プール・浅い水 (控えめな光の網目) | `M_Water_Pool` |
| `Water_Normal_Cyber_01.png` | 近未来・ホログラム水面 (細いデータ流) | `M_Water_Cyber` |

すべて **1024×1024 PNG / シームレス / インポート設定済み (NormalMap・Repeat・Android/iOS は ASTC 6x6)**。
マテリアルは Standard シェーダー(Metallic 0 / Smoothness 高め / 不透明)なので、
ビルトイン RP と VRChat (PC / Quest ワールド) でそのまま使えます。

### 一瞬で水面にする 3 つの方法

1. **右クリック一発**: Hierarchy でオブジェクトを選択 → 右クリック →
   `Siliq Water > 水マテリアルを適用 > 好きな水` — マテリアル適用と同時に
   `WaterSurfaceAnimator` コンポーネントも自動で付き、**再生すると波が流れます**
   `波紋 (Ripple)` だけは例外で、ノーマルを横へ流さず、
   `WaterRippleEmitter` を自動で付けて発生点から外へ広がるリングを作ります。
   PC 向けに透ける水が欲しい場合は
   `Siliq Water > 透明な水マテリアルを適用 (PC) > 好きな水` を使ってください。
   `Assets/SiliqWater/GeneratedMaterials/` に透明設定済みのマテリアルを生成して適用します。
   iOS / モバイル向けに軽い透明水が欲しい場合は
   `Siliq Water > 透明な水マテリアルを適用 (iOS/Mobile) > 好きな水` を使ってください。
   同梱の `Siliq/Water Mobile (Quest)` を alpha blend 設定にしたマテリアルを生成します。
   正面は透け、斜め視線では Fresnel で反射と不透明感が増え、透過光・細い光・きらめきで水らしさが出るように調整済みです。
2. **ドラッグ & ドロップ**: `PrebakedPack/Materials/` の `M_Water_*` をシーンのオブジェクトへドラッグ
   (この方法では静止したままなので、動かしたい場合は次項のコンポーネントを手動で追加してください)
3. **サンプルシーンで見比べる**: `PrebakedPack/SampleScene/SC_WaterNormalMap_Preview.unity` を開くと
   5 種の水面が Plane に貼られた状態で比較できます (このシーンは静止状態です)

### 水面を動かす (WaterSurfaceAnimator)

`PrebakedPack` のマテリアルは Standard シェーダーで UV アニメーション機能を持たないため、
静止画のままだと波が流れません。**右クリック適用なら自動で付与**されますが、
手動でドラッグ&ドロップした場合は `Runtime/Components/WaterSurfaceAnimator.cs` を
対象オブジェクトにアタッチしてください。Standard / URP Lit / VRChat Mobile など、
対象プロパティ (`_BumpMap` 等) を持つシェーダーであれば動作します。

インスペクタで次の 4 項目をスライダーで直感的に調整できます。

| 項目 | 内容 |
|---|---|
| **方向 (度)** | 波が流れる向き。0=右、90=上、180=左、270=下 |
| **速さ** | 流れる速さ。0 で静止 |
| **強さ** | 凹凸の強さ (シェーダーに `_BumpScale` がある場合) |
| **模様の大きさ** | 1 が元のサイズ、大きいほど模様が細かく見える |

**Play ボタンを押さなくても、値を変えるとシーンビュー上でその場に反映**されます。
Standard / URP Lit / VRChat Mobile 系では `_BumpMap` の UV と `_BumpScale` を、
同梱の Siliq 水シェーダーでは `_Scroll1` / `_Scroll2` / `_NormalStrength` / `_Tiling*` を
`MaterialPropertyBlock` 経由で動かすため、共有マテリアルを汚さずに調整できます。
右クリック適用時は、静止して見えない問題を避けるため水の種類ごとに少し強めの初期値が入ります。
ただし `波紋 (Ripple)` はスライドさせると水滴の波紋として不自然なので、
このコンポーネントの速度は 0 にし、下記の `WaterRippleEmitter` で同心円が広がる表現にしています。

より本格的な (2 レイヤースクロール・反射・岸辺フォームなどを含む) 動く水面が欲しい場合は、
下記の `Siliq/Water Mobile (Quest)` を使ってください。URP プロジェクトでは
Package Manager の `URP Shader` Sample を Import すると `Siliq/Water URP` も使えます。
これらは最初から UV スクロールが組み込まれているため `WaterSurfaceAnimator` は不要です。

見た目は `PrebakedPack/Preview/` のプレビュー画像 (Plane に貼って光を当てた状態) で事前確認できます。

### 手動でインポート設定する場合

パックの .meta を使わず PNG だけコピーした場合は、以下を設定してください。

```
Texture Type : Normal map
Wrap Mode    : Repeat
sRGB         : Off (Normal map タイプなら自動)
Filter Mode  : Bilinear または Trilinear
Max Size     : 1024
Compression  : Normal Quality (Quest は Android オーバーライドで ASTC 6x6)
```

### 改変方法

各パック画像は本ツールのプリセットと 1:1 対応しています。水面マップスタジオで
「プール (光の網目)」「サイバー (細いデータ流)」等のプリセットを適用 → パラメータやシードを
変更して書き出せば、同系統のバリエーションを自作できます。
プールは強い法線凹凸ではなく、浅く細い光の揺らぎとして見せる想定です。
`WaterSurfaceAnimator` の **強さ** を上げすぎると、光網ではなく太い凹凸に見えます。
サイバーは太い格子模様やセル境界ではなく、細いデータ流と斜めスキャン光の SF 水面として使う想定です。

### 透明な水にしたい場合 (PC 向け)

Quest / モバイルでは不透明のまま使うことを推奨します。PC 専用で透明にする場合は
右クリックメニューの `透明な水マテリアルを適用 (PC)` / `透明な水マテリアルを適用 (iOS/Mobile)` を使うか、水面マップスタジオの
自動作成マテリアルで **透明マテリアルとして作成** を ON にしてください。
綺麗な海、透明プール、血の海、液体金属のような用途が決まっている場合は、
`Siliq Water > 用途別マテリアルを適用` から見た目プリセットを選ぶと、
ノーマル、色、透明度、反射、動きまでまとめて設定できます。
ノーマルマップ単体は凹凸だけを表すため、透明感はマテリアルの Blend / Alpha / `_Opacity`
と Fresnel 連動の `_AlphaFresnel` / `_EdgeReflection` で作ります。
iOS 透明版はさらに `_TransmissionStrength` / `_GlimmerIntensity` / `_GlintIntensity` で
透過光、細い光の揺らぎ、強いハイライトを足し、透明なだけの板に見えにくい設定にしています。
iOS 透明版は GrabPass や深度依存なしの alpha blend なので軽量ですが、
透明描画はソート順と重なりに弱い点に注意してください。Quest 用の不透明運用と混ぜないこと。

## インストール

### UPM (Git URL) — 推奨

`Window > Package Manager > + > Add package from git URL...`:

```
https://github.com/shizuya1224/siliq_water.git
```

### 手動

このリポジトリを丸ごと `Packages/` フォルダ、または `Assets/SiliqWater/` にコピーしてください。

## 使い方

1. メニューの **Tools > Siliq Water > 水面マップスタジオ** を開く
2. **プリセット**から近いものを選んで「適用」
3. プレビュータブ(ノーマル / ハイト / フォーム / ラフネス / フロー / DUDV / コースティクス)を切り替えながらレイヤーを調整
4. 書き出したいマップをボタンで選択 → **「選択したマップを一括書き出し」**
   - 「マテリアルを自動作成」を選んでおくと、割り当て済みマテリアルも一緒に生成されます
5. 気に入った設定は**プロファイルとして保存**(チーム共有・再編集用)

## ランタイム生成 API

コアは UnityEditor 非依存 (`Siliq.Water.Runtime`) なので、実行時に生成できます。

```csharp
using Siliq.Water;

// プロファイルからベイク
Texture2D normal = profile.Bake(WaterMapType.Normal, 512);

// あるいは設定を直接組み立てて
var settings = WaterMapPresets.Create(1); // 海のうねり
settings.globalSeed = Random.Range(0, 999999); // 起動ごとに違う水面
Texture2D foam = WaterMapCore.BakeTexture(settings, WaterMapType.Foam, 512);
```

`RuntimeWaterMapApplier` コンポーネントを Renderer に付ければ、コード無しで
「起動時にプロファイルからベイクしてマテリアルへ適用」まで行えます(シードのランダム化対応)。
起動時の停止を避けたい場合は `generateAsync` を ON にすると、色計算をバックグラウンドで行い、
同じ設定・解像度の生成結果は `useTextureCache` によりシーン内で使い回されます。

## 同梱シェーダー

### `Siliq/Water URP` (URP 用 / 任意 Sample)

SRP Batcher 対応・1 パス。URP が入っているプロジェクトだけで使う任意 Sample です。
Built-in / VRChat / Quest / iOS 向けの通常導入ではコンパイル対象にしないため、URP package が無いプロジェクトでも import error を起こしません。

導入:

`Package Manager > Siliq Water Maps Studio > Samples > URP Shader > Import`

- ノーマルマップ 2 レイヤースクロール、または**フローマップ駆動**の流れ(生成したフローマップをそのまま活用)
- リフレクションプローブによる映り込み + フレネル + スペキュラ
- **深度ベースの岸辺エフェクト**(浅瀬の色変化・岸辺フォームライン・水際の透明化) — URP 設定で Depth Texture を ON にして使用

### `Siliq/Water Mobile (Quest / iOS)` (ビルトイン RP 用)

1 パス・GrabPass なしのモバイル向け設計。通常は不透明、透明マテリアル作成時は
`_Opacity` / `_AlphaFresnel` / `_EdgeReflection` / `_SrcBlend` / `_DstBlend` / `_ZWrite` を
切り替えて iOS でも透ける水面にできます。

- ノーマルマップ 1 枚を 2 回スクロールサンプリング
- 深い色 ⇔ 浅い色 + フレネル + 透過光 + 細い光の揺らぎ + スペキュラ + 任意のキューブマップ反射
- `_MacroVariation` / `_MacroScale` / `_MacroDirectionBreakup` /
  `_MacroColorVariation` により、大きな面でも模様の密度・向き・光が均一になりすぎないよう調整

両シェーダーとも、マテリアルの **「触れた時の波紋を有効化」** を ON にすると、
下記のインタラクティブな波紋機能が使えるようになります。
複数の水面を独立させたい場合は、水面マテリアルの `_RippleChannel` と
`WaterRippleSource` の `rippleChannel` を同じ番号にしてください。

## インタラクティブな波紋 (アバターが入ると水面が変わる)

同梱の 2 シェーダーは、指定したワールド座標から**実時間で波紋が広がる**機能を持っています。
波紋の発生源 (アバターの接触位置など) は、以下いずれかのコンポーネントが供給します。

### 雨面・環境演出として自動で波紋を出す

`Runtime/Components/WaterRippleEmitter.cs` を水面 Renderer にアタッチすると、
Renderer の範囲内へ波紋発生点を自動で作ります。Play 中、各点からリングが外側へ広がります。
右クリックメニューの `波紋 (Ripple)` はこの方式を自動設定します。

調整項目:

| 項目 | 内容 |
|---|---|
| **Ripples Per Second** | 1秒あたりに発生する波紋数 |
| **Burst Count** | 同時に出す波紋数 |
| **Ripple Speed** | リングが外へ広がる速さ |
| **Ripple Width** | リング幅。小さいほど細く鋭い輪 |
| **Ripple Lifetime** | 消えるまでの秒数 |
| **Ripple Amplitude** | 法線に乗せる波紋の強さ |

### 通常の Unity プロジェクト / エディタでのテスト

`Runtime/Components/WaterRippleSource.cs` を、水面の範囲をカバーする
**Is Trigger 付きコライダー**のオブジェクトにアタッチしてください。

1. マテリアルの「触れた時の波紋を有効化」を ON にする
2. 水面オブジェクト (または水面上の透明な判定用オブジェクト) に Trigger コライダーを付け、
   `WaterRippleSource` をアタッチ
3. `waterSurfaceY` を水面の実際のワールド Y 座標に合わせる
4. Play すると、Collider を持つ他のオブジェクト (アバター等) がその範囲に入るたびに、
   接触位置から波紋が広がります

波の広がる速さ・幅・持続時間・強さはマテリアル側のプロパティ
(`_RippleSpeed` / `_RippleWidth` / `_RippleLifetime` / `_RippleAmplitude`) で調整できます。

### VRChat ワールドの場合 (要 UdonSharp)

**重要:** VRChat にアップロードしたワールドでは通常の MonoBehaviour は実行されず、
Udon (UdonSharp) のみが動作します。そのため上記の `WaterRippleSource.cs` は
**エディタでのテスト用**であり、実際にアップロードしたワールドでは機能しません。

VRChat ワールドで全プレイヤーの水面インタラクションを再現するには、
`Runtime/VRChatSupport/WaterRippleSourceUdon.cs.txt` を使用してください。
このファイルはあえて `.cs.txt` にしてあり (VRChat SDK が無い環境でもビルドが壊れないようにするため)、
中身をコピーして UdonSharp スクリプトとして作り直す必要があります。手順はファイル冒頭のコメントに
記載しています。VRChat の全プレイヤー位置は SDK 側で既に同期済みのため、追加のネットワーク同期は不要です。

> **VRChat 向け注意点:** Udon 版は `.cs.txt` のテンプレートとして同梱しています。
> VRChat SDK / UdonSharp 導入済みプロジェクトで `.cs` として配置し、
> ClientSim または実機で確認してからワールドへ組み込んでください。

## VRChat モバイル (Quest) での使い方

| 用途 | 方法 |
|---|---|
| **アバター** | Quest アバターはカスタムシェーダー不可のため、`VRChat/Mobile/Standard Lite` の **Normal Map** スロットに生成したマップをセット |
| **ワールド** | カスタムシェーダー可。`Siliq/Water Mobile (Quest)` に生成マップをセットすれば動く水面に |

「モバイル向けインポート設定を自動適用」を ON にしておくと、Android / iOS 両プラットフォームの
テクスチャ圧縮が **ASTC 6x6・最大 1024px** に設定され、Quest や iPhone のメモリ制限に収まりやすくなります。
PrebakedPack のテクスチャにも同設定が最初から入っています。

## iOS 対応

通常同梱の `Siliq/Water Mobile` は標準的な CG/HLSL のみで書かれており、
Android 専用の API には依存していないため Metal (iOS) でもそのままコンパイル・動作します。
`Siliq/Water URP` は URP Sample を Import した URP プロジェクトでのみ使用してください。
テクスチャのインポート設定にも iOS (`iPhone`) 向けの ASTC 6x6 圧縮が含まれています。

VRChat の iOS 版クライアント自体の対応状況はアプリ側の仕様に依存するため、
実際にアップロードする際は VRChat SDK の Quest/iOS ビルド対象設定に従ってください。

## トラブルシューティング

### `package.json has no meta file` が残る

このリポジトリには `package.json.meta` を同梱しています。警告が残る場合、Unity が古い PackageCache または古い `Packages/packages-lock.json` の commit を見ている可能性が高いです。

対処:

1. Package Manager から `Siliq Water Maps Studio` を Remove
2. プロジェクトの `Packages/packages-lock.json` から古い `com.siliq.water-normalmap` の参照を更新、または削除して再解決
3. 必要なら `Library/PackageCache/com.siliq.water-normalmap*` を削除
4. Git URL を最新 commit で Add し直す

### `Core.hlsl` が見つからない

Built-in / VRChat プロジェクトに URP package が入っていない状態で `Siliq/Water URP` を直接 import すると発生します。通常導入では URP shader は読み込まれません。

- Built-in / VRChat / Quest / iOS: `Siliq/Water Mobile (Quest)` を使う
- URP: Universal Render Pipeline を導入した上で `Samples > URP Shader` を Import する

## 構成

```
Runtime/
  Core/
    WaterMapTypes.cs        レイヤー・設定・マップ種の定義
    WaterMapCore.cs         生成コア (タイリングノイズ / 7 種マップ変換 / ベイク API)
    WaterMapPresets.cs      プリセット定義
    WaterMapProfile.cs      設定の ScriptableObject プロファイル
  Components/
    RuntimeWaterMapApplier.cs  ランタイムベイク & 適用コンポーネント
  Shaders/
    SiliqWaterMobile.shader    ビルトイン RP / Quest ワールド向け
Editor/
  WaterMapStudioWindow.cs   エディタウィンドウ (プレビュー / 書き出し / プロファイル)
Samples~/
  URP/
    SiliqWaterURP.shader       URP 向け任意 Sample (フローマップ・岸辺エフェクト対応)
Tests/
  WaterMapCoreTests.cs      Unity Test Runner (EditMode) 用の自動テスト
PrebakedPack/
  Textures/                 焼き済みノーマルマップ 5 種 (1024px, インポート設定済み)
  Materials/                設定済み Standard マテリアル 5 種
  SampleScene/              SC_WaterNormalMap_Preview.unity (5 種比較シーン)
  Preview/                  Plane に貼った状態のプレビュー画像
```

## テスト

`Window > General > Test Runner` (EditMode) から実行できます。全プリセットについて
シームレスタイリング・完全ループ・値域・決定性・ベイク API を自動検証します。

## 技術メモ

- 全ノイズ(パーリン fBm / ボロノイ / 正弦波合成 / 波紋)は整数周期のトーラス上で定義され、
  タイリングとループが**構造的に保証**されています(後処理のブレンドで誤魔化していません)
- アニメーションの位相・スクロール速度・セル揺らぎもすべて整数周期で、t ∈ [0,1) が正確に 1 周します
- 生成は CPU 並列 (`Parallel.For`)。1024px・数レイヤーで数秒以内です

## ライセンス

このリポジトリの所有者に帰属します。
