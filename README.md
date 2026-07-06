# Siliq Water Maps Studio

Unity エディタ上(またはランタイム)で、水面・流体表現向けの **PBR テクスチャ一式を手続き生成**するツールです。

- **VRChat モバイル (Quest / Android / iOS)** — 生成物はただの PNG テクスチャなのでそのまま使用可
- **最新の Unity (ビルトイン / URP)** — フォーム・フロー・ラフネスマップまで含む本格的な水表現に対応
- **ランタイム API** — ビルドにテクスチャを含めず、ロード時に動的生成することも可能
- **触れたら波紋が広がるインタラクティブな水面** — アバターが水に入った位置から波紋が実時間で広がる (同梱シェーダー限定)

対応: Unity 2019.4 以降 / ビルトイン RP・URP

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
- **プリセット 8 種** — 湖 / 海 / 外洋 (スペクトル) / 川 / 雨 / さざ波 / トゥーン / 溶岩
- **プロファイル (ScriptableObject)** — 設定をアセットとして保存・共有。JSON コピー & ペーストにも対応
- **マテリアル自動作成** — 書き出したマップを Standard / URP Lit / 同梱水シェーダーへ割り当て済みのマテリアルを生成
- **モバイル向けインポート設定の自動適用** — Repeat / NormalMap タイプ / Android=ASTC 6x6 (VRChat Quest 向け)

## 焼き済みパック (PrebakedPack) — ツール不要ですぐ使える 5 種

ツールを触らなくても、`PrebakedPack/` に**すぐ使える水・素材ノーマルマップ 11 種 + 設定済みマテリアル + サンプルシーン**が入っています。

**水の表現 (5 種)**

| ファイル | 用途 | マテリアル |
|---|---|---|
| `Water_Normal_Calm_01.png` | 静かな湖・穏やかな水面 | `M_Water_Calm` |
| `Water_Normal_Ripple_01.png` | 雨の波紋 | `M_Water_Ripple` |
| `Water_Normal_Stream_01.png` | 川・一方向に流れる水 | `M_Water_Stream` |
| `Water_Normal_Pool_01.png` | プール・浅い水 (光の網目) | `M_Water_Pool` |
| `Water_Normal_Cyber_01.png` | 近未来・人工水面 | `M_Water_Cyber` |

**素材の表現 (6 種) — 透明水〜ガラス〜金属**

| ファイル | 用途 | マテリアル |
|---|---|---|
| `Water_Normal_Shallows_01.png` | 透き通った浅瀬の煌めき | `M_Water_Shallows` |
| `Water_Normal_FlutedGlass_01.png` | リブ (フルート) ガラス | `M_Water_FlutedGlass` |
| `Water_Normal_LiquidMetal_01.png` | 液体金属・クローム | `M_Water_LiquidMetal` |
| `Water_Normal_FrostedGlass_01.png` | すりガラス (マット) | `M_Water_FrostedGlass` |
| `Water_Normal_Condensation_01.png` | 窓の結露 (水滴) | `M_Water_Condensation` |
| `Water_Normal_Kaleidoscope_01.png` | 万華鏡・カットクリスタル ※非タイリング | `M_Water_Kaleidoscope` |

すべて **1024×1024 PNG / インポート設定済み (NormalMap・Android/iOS は ASTC 6x6)**。
万華鏡以外はシームレスにタイリングします (万華鏡は中心対称のため Clamp・装飾パネル向け)。
マテリアルは Standard シェーダー(Metallic 0 / Smoothness 高め / 不透明)なので、
ビルトイン RP と VRChat (PC / Quest ワールド) でそのまま使えます。
金属らしさが欲しい場合はマテリアルの Metallic / Smoothness を上げてください。

### 一瞬で水面にする 3 つの方法

1. **右クリック一発**: Hierarchy でオブジェクトを選択 → 右クリック →
   `Siliq Water > 水マテリアルを適用 > 好きな水` — マテリアル適用と同時に
   `WaterSurfaceAnimator` コンポーネントも自動で付き、**再生すると波が流れます**
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

**Play ボタンを押さなくても、値を変えるとシーンビュー上でその場に反映**されます
(エディタ編集中もアニメーションし続けるため、確認しながら調整できます)。

より本格的な (2 レイヤースクロール・反射・岸辺フォームなどを含む) 動く水面が欲しい場合は、
下記の同梱シェーダー `Siliq/Water Mobile (Quest)` や `Siliq/Water URP` を使ってください。
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
「プール (光の網目)」「サイバー (人工水面)」等のプリセットを適用 → パラメータやシードを
変更して書き出せば、同系統のバリエーションを自作できます。

### 透明な水にしたい場合 (PC 向け)

Quest / モバイルでは不透明のまま使うことを推奨します。PC 専用で透明にする場合は
マテリアルを複製して `M_Water_Calm_PC_Transparent` のように別名にし、
Rendering Mode を Transparent へ変更してください (Quest 用と混ぜないこと)。

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

## 同梱シェーダー

### `Siliq/Water URP` (URP 用)

SRP Batcher 対応・1 パス。最新 Unity での本命です。

- ノーマルマップ 2 レイヤースクロール、または**フローマップ駆動**の流れ(生成したフローマップをそのまま活用)
- リフレクションプローブによる映り込み + フレネル + スペキュラ
- **深度ベースの岸辺エフェクト**(浅瀬の色変化・岸辺フォームライン・水際の透明化) — URP 設定で Depth Texture を ON にして使用

### `Siliq/Water Mobile (Quest)` (ビルトイン RP 用)

1 パス・不透明・GrabPass なしの Quest セーフ設計。VRChat ワールドに最適。

- ノーマルマップ 1 枚を 2 回スクロールサンプリング
- 深い色 ⇔ 浅い色 + フレネル + スペキュラ + 任意のキューブマップ反射

両シェーダーとも、マテリアルの **「触れた時の波紋を有効化」** を ON にすると、
下記のインタラクティブな波紋機能が使えるようになります。

## インタラクティブな波紋 (アバターが入ると水面が変わる)

同梱の 2 シェーダーは、指定したワールド座標から**実時間で波紋が広がる**機能を持っています。
波紋の発生源 (アバターの接触位置など) は、以下いずれかのコンポーネントが供給します。

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

> **正直な注意点:** 私 (Claude) の制作環境には VRChat SDK / UdonSharp が無く、
> この Udon スクリプトは実際のコンパイル・ClientSim・実機での動作確認ができていません。
> API 名や引数が実際のものと食い違っている可能性があります。試してエラーが出た場合は
> 内容を教えてください、修正します。

## VRChat モバイル (Quest) での使い方

| 用途 | 方法 |
|---|---|
| **アバター** | Quest アバターはカスタムシェーダー不可のため、`VRChat/Mobile/Standard Lite` の **Normal Map** スロットに生成したマップをセット |
| **ワールド** | カスタムシェーダー可。`Siliq/Water Mobile (Quest)` に生成マップをセットすれば動く水面に |

「モバイル向けインポート設定を自動適用」を ON にしておくと、Android / iOS 両プラットフォームの
テクスチャ圧縮が **ASTC 6x6・最大 1024px** に設定され、Quest や iPhone のメモリ制限に収まりやすくなります。
PrebakedPack のテクスチャにも同設定が最初から入っています。

## iOS 対応

同梱シェーダー (`Siliq/Water Mobile`・`Siliq/Water URP`) は標準的な CG/HLSL のみで書かれており、
Android 専用の API には依存していないため Metal (iOS) でもそのままコンパイル・動作します。
テクスチャのインポート設定にも iOS (`iPhone`) 向けの ASTC 6x6 圧縮が含まれています。

VRChat の iOS 版クライアント自体の対応状況はアプリ側の仕様に依存するため、
実際にアップロードする際は VRChat SDK の Quest/iOS ビルド対象設定に従ってください。

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
    SiliqWaterURP.shader       URP 向け (フローマップ・岸辺エフェクト対応)
Editor/
  WaterMapStudioWindow.cs   エディタウィンドウ (プレビュー / 書き出し / プロファイル)
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
