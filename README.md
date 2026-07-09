# Siliq Water

Unity / VRChat向けの、**完成済み水面Materialをそのまま使う**水表現パッケージです。
通常利用ではTextureもMaterialも生成しません。海、透明プール、室内水面、ウォーターテーブル、血の海、液体金属から用途を選び、Rendererへドラッグするだけで動きます。

- **ドラッグ&ドロップ**: `PrebakedPack/ReadyMaterials/M_Siliq_*_Ready` をRendererへ貼る
- **右クリック一発設定**: Hierarchyで水面を選択し、`Siliq Water > 完成水面を適用` から用途を選ぶ
- **PC / Quest / iOS**: `Tools > Siliq Water > 完成マテリアル` で対象を選ぶ。共有Materialは複製・書き換えない
- **水面と水底を分離**: 水面は `M_Siliq_*_Ready`、水底光は床側の `M_Siliq_*_CausticsOverlay` を別メッシュへ貼る
- **完成Prefab**: 水面、分割メッシュ、床、水底光まで含む確認用セットをHierarchyへ置ける
- **広がる波紋**: 必要な場合だけ `WaterRippleSource` を使い、接触位置からリングを外側へ広げる

対応: Unity 2019.4以降 / Built-in RP / VRChat Quest・iOS。URP shaderはPackage Managerの任意Sampleです。

## 製品方針

製品の入口は `ReadyMaterials` と完成Prefabです。旧Standard確認Material、手続きマップ生成コア、ランタイム生成APIは既存プロジェクトとの互換用に残していますが、通常メニューには出しません。自動生成結果を完成Materialの代わりには使いません。

用途別Materialの選び方は [Docs/MaterialLookPresetGuide.md](Docs/MaterialLookPresetGuide.md)、公開前の確認項目は [Docs/PublicReleaseChecklist.md](Docs/PublicReleaseChecklist.md) を参照してください。

## 初心者はまずこれ

Unity / VRChat 初心者は、最初にこのメニューを使ってください。

`Tools > Siliq Water > はじめてガイド`

ガイド内のボタンから、最高品質 Hero 完成セットの配置、透明プール完成セットの配置、最高品質 Hero 水面の作成、クリスタルラグーン水面の作成、選択中の水面の診断修復、
README / 用途別ガイド / PrebakedPack の確認に進めます。

見た目を最優先で確認する場合は、床と水底光まで入った完成セットを先に配置してください。

`Tools > Siliq Water > かんたん作成 > 最高品質 Hero 完成セットを配置`

これだけで、分割済み水面メッシュ、Hero 専用 normal / height を使う透明水マテリアル、明るい床、
床用 Hero caustics overlay、確認用ライトをまとめて配置します。水面だけを作りたい場合は
`Tools > Siliq Water > かんたん作成 > 最高品質 Hero 水面を作成` を使います。1 枚 Quad では実高さが見えないため、
この完成セットまたは分割メッシュを基準にしてください。
このフローは専用の 2048px Hero normal map / height map を水面に使い、水底光は床用 caustics overlay の別メッシュで表現するため、Calm や通常 Crystal Lagoon の素材を流用しません。
プール用途で自然な透明感から始める場合は `Tools > Siliq Water > かんたん作成 > 透明プール完成セットを配置` を使います。ClearPool 水面、明るい床、SunlitPool 専用 caustics overlay、確認用ライトをまとめて配置します。

水底、プール床、水槽底の光は水面とは別のメッシュに付けます。床や水底の少し上へ薄い Plane を置き、`M_Siliq_CrystalLagoon_CausticsOverlay`、Hero では `M_Siliq_CrystalLagoon_Hero_CausticsOverlay`、透明プールでは `M_Siliq_SunlitPool_CausticsOverlay` を貼ってください。水面 Ready material は水面だけを担当します。

すでに作った水面がピンク、透明すぎる、動かない、高さが出ない場合は、対象を選択して次を実行します。

`Tools > Siliq Water > かんたん作成 > 選択中の水面を診断して自動修復`

未対応 shader、マテリアル未設定、Animator なし、頂点不足の Plane / Quad を自動で直します。
ノーマルマップだけでは高級な水には見えません。色、透明度、反射、ハイライト、実高さ、
ライト、メッシュ分割が揃って初めて水面として成立します。

## 公開リポジトリとしての状態

- `LICENSE.md`: 現時点では製品向けの All rights reserved。配布条件を変える場合は公開前に差し替えてください。
- `SECURITY.md`: 脆弱性や secret 露出の非公開報告ルール。
- `CONTRIBUTING.md`: 変更時の品質基準と検証手順。
- `.gitignore` / `.gitattributes`: Unity 生成物の混入防止と text/binary 管理。

## 焼き済みパック (PrebakedPack) — ツール不要ですぐ使える基本 5 種 + 旗艦 + 完成 Prefab

ツールを触らなくても、`PrebakedPack/` に**すぐ使える水ノーマルマップ基本 5 種 + フラッグシップ専用 normal / height + アタッチ用完成マテリアル + 水底用 caustics overlay + Crystal Lagoon / SunlitPool 完成 Prefab + サンプルシーン**が入っています。

| ファイル | 用途 | 確認用マテリアル |
|---|---|---|
| `Water_Normal_Calm_01.png` | 静かな湖・穏やかな水面 | `M_Water_Calm` |
| `Water_Normal_Ripple_01.png` | 雨の波紋 | `M_Water_Ripple` |
| `Water_Normal_Stream_01.png` | 川・一方向に流れる水 | `M_Water_Stream` |
| `Water_Normal_Pool_01.png` | プール・浅い水 (控えめな光の網目) | `M_Water_Pool` |
| `Water_Normal_Cyber_01.png` | 近未来・ホログラム水面 (細いデータ流) | `M_Water_Cyber` |
| `Water_Normal_FlagshipCrystal_01.png` + `Water_Height_FlagshipCrystal_01.png` | 製品デモ向けの透明水・高反射・実高さ | `M_Water_FlagshipCrystal` |
| `Water_Normal_CrystalLagoon_01.png` + `Water_Height_CrystalLagoon_01.png` | 透き通った浅い水・美しさ特化 | `M_Siliq_CrystalLagoon_Ready` |
| `Water_Normal_CrystalLagoon_Hero_01.png` + `Water_Height_CrystalLagoon_Hero_01.png` | 最高品質確認用の透明水・柔らかい波面 | `M_Siliq_CrystalLagoon_Hero_Ready` |
| `Water_Normal_WaterTable_01.png` + `Water_Height_WaterTable_01.png` | ガラス水盤・ウォーターテーブル用の中央リング波 | `M_Siliq_WaterTable_Ready` |
| `Water_Caustics_CrystalLagoon_01.png` | 水底/床メッシュに重ねる Crystal Lagoon 専用 caustics | `M_Siliq_CrystalLagoon_CausticsOverlay` |
| `Water_Caustics_CrystalLagoon_Hero_01.png` | 水底/床メッシュに重ねる Hero 専用 caustics | `M_Siliq_CrystalLagoon_Hero_CausticsOverlay` |
| `Water_Caustics_SunlitPool_01.png` | 透明プール/室内プールの床に重ねる柔らかい caustics | `M_Siliq_SunlitPool_CausticsOverlay` |

基本 5 種は **1024×1024 PNG / シームレス / インポート設定済み (NormalMap・Repeat・Android/iOS は ASTC 6x6)**。
フラッグシップ透明水、Crystal Lagoon、WaterTable は専用 **2048×2048 normal map + 2048×2048 height map** を同梱し、用途別 Quick Apply では両方を Siliq 水シェーダーへ割り当てます。
WaterTable は水面専用です。ガラス天板、LED ライン、ベース、中央ノズルは別オブジェクトで作り、水面 material に飛沫や塩のような後付け要素は混ぜません。
Crystal Lagoon / Hero / SunlitPool の caustics は水面 material ではなく、床や水底用の別メッシュへ貼る overlay material で使います。水面に白い線や塩のような模様を混ぜないため、Ready material 側の `_CausticsStrength` / `_BottomVisibility` / `_BottomLightStrength` / `_BottomGlowStrength` は初期値 0 です。
共通の `Water_Caustics_Crystal_01.png` も同梱していますが、水面に直接混ぜる用途ではなく、必要な時だけ別メッシュ/別material側で使う前提です。
`PrebakedPack/ReadyMaterials/` には `Siliq/Water Mobile (Quest)` 設定済みの完成マテリアルが入っています。
`PrebakedPack/Prefabs/PF_Siliq_CrystalLagoon_Complete.prefab` は、分割済み水面、明るいプール床、床用 caustics overlay、確認用ライトを一体化した完成セットです。さらに `PF_Siliq_CrystalLagoon_Hero_Complete.prefab` は Hero material と Hero caustics overlay を貼った最高品質確認用です。`PF_Siliq_SunlitPool_Complete.prefab` は ClearPool material と SunlitPool caustics overlay を使う、透明プール / 室内プール向けの完成セットです。Prefab を Hierarchy へ置くだけで、透明感と水底光を同時に確認できます。
完成形の方向性は `PrebakedPack/Preview/preview_crystal_lagoon_complete.png`、最高品質寄りは `PrebakedPack/Preview/preview_crystal_lagoon_hero_complete.png` で確認できます。薄い床、透明な水面、窓の揺れる反射、水底光が一枚で見える初心者向けの目安画像です。

| Ready material | 用途 |
|---|---|
| `M_Siliq_ClearSea_Ready` | 綺麗な海・透明感のある水面 |
| `M_Siliq_ClearPool_Ready` | 透明プール |
| `M_Siliq_IndoorBluePool_Ready` | 明るい室内プール |
| `M_Siliq_WaterTable_Ready` | ガラス水盤・ウォーターテーブルの黒青い浅い水面 |
| `M_Siliq_FlagshipCrystal_Ready` | 製品デモ向けのフラッグシップ透明水 |
| `M_Siliq_CrystalLagoon_Ready` | 透き通った美しさ特化の水面 |
| `M_Siliq_CrystalLagoon_Hero_Ready` | 専用 Hero normal / height で透明感・反射を強めた最高品質確認用の水面 |
| `M_Siliq_PalePoolFloor` | Crystal Lagoon 完成 Prefab 用の明るい床。薄いタイル感と水底光の受け皿を持つ |
| `M_Siliq_CrystalLagoon_CausticsOverlay` | プール床・浅い海底に重ねる Crystal Lagoon 専用の水底光 |
| `M_Siliq_CrystalLagoon_Hero_CausticsOverlay` | Hero 完成 Prefab 用の強めの床用水底光 |
| `M_Siliq_SunlitPool_CausticsOverlay` | 透明プール / 室内プールの床に重ねる柔らかい SunlitPool 水底光 |
| `M_Siliq_BloodSea_Ready` | 血の海・赤い液体 |
| `M_Siliq_LiquidMetal_Ready` | 液体金属 |

美しさを最優先する場合は、まず `PF_Siliq_CrystalLagoon_Hero_Complete.prefab` か `M_Siliq_CrystalLagoon_Hero_Ready` から確認してください。
これらは normal map、色、透明度、反射、scroll、実高さの初期値まで設定済みなので、
Renderer にドラッグ&ドロップするだけで水として動きます。`WaterSurfaceAnimator` は必須ではありません。
床そのものに光を出したい場合は、水底の少し上に薄い Plane を置き、`M_Siliq_CrystalLagoon_CausticsOverlay`、Hero では `M_Siliq_CrystalLagoon_Hero_CausticsOverlay`、透明プールでは `M_Siliq_SunlitPool_CausticsOverlay` を貼ります。これは `Siliq/Caustics Overlay Mobile` を使う軽量な加算 material で、専用 caustics を床に重ねます。床用 overlay は `_Focus` / `_SoftScatter` / `_PrismStrength` を持ち、焦点線、柔らかい散光、薄い色分散を調整できます。
既存の `PrebakedPack/Materials/M_Water_*` は Standard シェーダーの互換・確認用です。

### 一瞬で水面にする4つの方法

1. **Materialを貼る**: `PrebakedPack/ReadyMaterials/` の `M_Siliq_*_Ready` をRendererへドラッグする。Material内のscrollで、そのまま波が動きます。
2. **右クリックで設定する**: Hierarchyで水面を選択し、`Siliq Water > 完成水面を適用` から用途を選ぶ。Package内の同じ完成Materialを直接割り当て、調整用 `WaterSurfaceAnimator` を追加します。`GeneratedMaterials` は作りません。
3. **完成Prefabを置く**: 最高品質確認は `PF_Siliq_CrystalLagoon_Hero_Complete.prefab`、通常確認は `PF_Siliq_CrystalLagoon_Complete.prefab`、透明プール確認は `PF_Siliq_SunlitPool_Complete.prefab` をHierarchyへドラッグする。
4. **水底光を別に足す**: 床や水底の少し上へ薄いPlaneを置き、用途に合う `M_Siliq_*_CausticsOverlay` を貼る。水面Materialへ白線や発光模様を混ぜません。

`PrebakedPack/SampleScene/SC_CrystalLagoon_Showcase.unity` では、分割済み水面、明るい床、床用caustics overlay、ライト、カメラを含む完成状態を確認できます。

### 水面を動かす (WaterSurfaceAnimator)

`PrebakedPack/ReadyMaterials/` の `M_Siliq_*_Ready` は shader 側に UV アニメーションが入っているため、基本的にこのコンポーネントは不要です。
一方で `PrebakedPack/Materials/` の `M_Water_*` は Standard シェーダーで UV アニメーション機能を持たないため、
静止画のままだと波が流れません。**右クリック適用なら自動で付与**されますが、
手動でドラッグ&ドロップした場合は `Runtime/Components/WaterSurfaceAnimator.cs` を
対象オブジェクトにアタッチしてください。Standard / URP Lit / VRChat Mobile など、
対象プロパティ (`_BumpMap` 等) を持つシェーダーであれば動作します。

インスペクタで動き、色、透明・反射を直接調整できます。
初期値は低速ですが、止まって見えない値にしています。速く見える場合は `Speed` だけでなく、Siliq 水マテリアル側の `_Scroll1` / `_Scroll2`、高さの `_DisplacementSpeed` も下げてください。水底光は別メッシュの overlay material 側で調整します。

| 項目 | 内容 |
|---|---|
| **方向 (度)** | 波が流れる向き。0=右、90=上、180=左、270=下 |
| **速さ** | 流れる速さ。0 で静止。0.3 が静かな水面の中間操作値。0.15-0.2 が完成Prefabの低速基準 |
| **Edit Mode Preview Fps** | 編集中プレビューの更新回数。低いほど軽い |
| **強さ** | 凹凸の強さ (シェーダーに `_BumpScale` / `_NormalStrength` がある場合) |
| **模様の大きさ** | 1 が元のサイズ、大きいほど模様が細かく見える |
| **高さ** | Siliq 水シェーダーで水面メッシュを実際に上下させる量 |
| **高さの波長 / 速度** | 実高さのうねりの大きさと動き |
| **ハイトマップの影響** | 書き出した Height map を頂点変位に使う割合 |
| **明るい水色 / 深い水色** | 水面の基本色。Standard / URP Lit では明るい水色がベースカラーになる |
| **反射色** | 空や環境が映り込む色 |
| **透過光** | 透明水の内側から出る色。水の厚みと透明感を作る |
| **透明な抜け感** | 水色の濁りを抑え、透過光と奥行きを見えやすくする |
| **不透明度** | 1 に近いほど濃く、低いほど透ける |
| **輪郭反射 / 反射量** | 斜め視線の反射と全体の映り込み |
| **反射パターン** | 空や窓の帯が水面に映る量と大きさ |
| **透過光量 / ハイライト / きらめき** | 透明感、強い光、細い揺らぎの量 |
| **暗所** | 暗い部屋や夜寄りのシーンで、反射の浮き、細部の光りすぎを調整 |
| **水底の光** | 通常は 0 のままにします。水底/床の caustics は別メッシュの overlay material で調整します |
| **水底の見え方** | 通常は 0 のままにします。透明な抜け感は `Opacity` / `Clarity` / `Refraction` / `Depth Tint` で調整します |
| **水底光の透け** | 通常は 0 のままにします。水面に塩のような白線を混ぜないための分離項目です |

**Play ボタンを押さなくても、値を変えるとシーンビュー上でその場に反映**されます。
Standard / URP Lit / VRChat Mobile 系では `_BumpMap` の UV、`_BumpScale`、色 alpha を、
同梱の Siliq 水シェーダーでは `_Scroll1` / `_Scroll2` / `_NormalStrength` / `_Tiling*` /
`_DisplacementStrength` / `_DisplacementScale` / `_DisplacementSpeed` /
`_CausticsStrength` / `_CausticsScale` / `_CausticsSpeed` / `_CausticsFocus` / `_CausticsPrismStrength` / `_CausticsScatterStrength` / `_BottomVisibility` / `_BottomLightStrength` / `_BottomGlowStrength` / `_DepthTintStrength` を
`_ShallowColor` / `_DeepColor` / `_HorizonColor` / `_TransmissionColor` /
`_Opacity` / `_Clarity` / `_RefractionStrength` / `_EdgeReflection` / `_ReflStrength` / `_ReflectionPatternStrength` / `_ReflectionPatternScale` /
`_MinLighting` / `_DarkReflectionDamping` / `_DarkDetailDamping` / `_CausticsTint` などと一緒に
`MaterialPropertyBlock` 経由で動かすため、共有マテリアルを汚さずに調整できます。Siliq 水シェーダーは `_ManualTime` も受け取り、Unity の Scene View 更新設定に依存せず編集時プレビューを動かせます。
実際の高さは頂点変位なので、1 枚ポリゴンの Quad では見えにくいです。Unity 標準の Plane や細分化された水面メッシュを使ってください。
PC の発熱を避けるため、編集モードの連続プレビューは**選択中の水面だけ**最大 10fps で更新されます。
重い場合は `Animate In Edit Mode` を OFF にするか、`Edit Mode Preview Fps` を下げてください。
右クリック適用時は、水の種類ごとに低速の初期値が入ります。Hero / Crystal Lagoon / 透明プールは 0.14-0.16 前後から始まり、止まって見えないが速く滑らない基準です。速く見える場合はまず **速さ** を 0.05-0.12、**高さの速度** を 0.00008 以下、**水底の光の速度** を 0.000012 以下まで下げてください。
水滴や接触の波紋は通常の水面スクロールとは別機能です。必要な場合だけ、下記の `WaterRippleEmitter` / `WaterRippleSource` で同心円が広がる表現を追加します。

より本格的な (2 レイヤースクロール・反射・岸辺フォームなどを含む) 動く水面が欲しい場合は、
下記の `Siliq/Water Mobile (Quest)` を使ってください。URP プロジェクトでは
Package Manager の `URP Shader` Sample を Import すると `Siliq/Water URP` も使えます。
これらは最初から UV スクロールが組み込まれているため `WaterSurfaceAnimator` は不要です。

見た目は `PrebakedPack/Preview/` のプレビュー画像で事前確認できます。
美しさ特化の入口は `preview_crystal_lagoon.png` です。透明感、水底の光、淡い青緑の方向性を先に確認できます。

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

フラッグシップ透明水の normal / height は 2048px を基準にしてください。Quest や iOS で軽くしたい場合だけ Android / iPhone override で 1024px に落とします。

### 改変方法

まず完成Materialをそのまま使い、調整が必要な場合だけProject内へ複製してください。Package内の共有Materialは右クリック適用やPC / Quest / iOS切り替えでは書き換えません。
プールは強い法線凹凸ではなく、浅い波面、反射、透明感、水底側のcausticsで見せます。Heroの水底光も水面へ混ぜず、柔らかい床光と少数の焦点線を別メッシュへ重ねます。
`WaterSurfaceAnimator` の **強さ** や **高さ** を上げすぎると、波ではなく固い模様や多角形に見えるため、同梱値を基準に少しずつ調整してください。

### 透明な水にしたい場合 (PC 向け)

`M_Siliq_ClearSea_Ready`、`M_Siliq_ClearPool_Ready`、`M_Siliq_CrystalLagoon_Ready`、`M_Siliq_CrystalLagoon_Hero_Ready` は透明設定済みです。別の透明Materialを自動生成する必要はありません。
Hierarchy右クリックの `Siliq Water > 完成水面を適用`、または `Tools > Siliq Water > 完成マテリアル` から選ぶと、同梱Materialを直接設定します。最高品質確認は `最高品質 クリスタルラグーン Hero` を選んでください。
ノーマルマップ単体は凹凸だけを表すため、透明感はマテリアルの Blend / Alpha / `_Opacity`
と Fresnel 連動の `_AlphaFresnel` / `_EdgeReflection` で作ります。
一枚の Plane を明るい Scene View 背景に置くだけだと、水の厚みや底面色が無いため薄く見えやすいです。
薄すぎる場合は用途別の `クリスタルラグーン Hero` / `クリスタルラグーン` / `美しい海` / `透明プール` / `室内ブループール` / `ウォーターテーブル` / `フラッグシップ透明水` を使ってください。
Quest / iOSを選んだ場合はMaterialを作り直さず、対象Rendererの高さ、反射、細部だけを軽量側へ調整します。
暗い場所で水面だけ明るく浮く場合は、WaterSurfaceAnimator の **暗所の反射抑制** と **暗所の細部抑制** を上げると、
空反射と細かい光が暗さに追従しやすくなります。暗すぎて水底光が沈む場合は **暗所の最低明るさ** を少し上げます。
iOS向け完成MaterialはGrabPassや深度依存なしのalpha blendなので軽量ですが、
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

1. メニューの **Tools > Siliq Water > 完成マテリアル** を開く
2. **PC / Quest / iOS** を選ぶ
3. **見た目**から、ウォーターテーブル / クリスタルラグーン Hero / クリスタルラグーン / 美しい海 / 透明プールなどを選ぶ
4. **選択中の水面へ設定** を押す
5. 水底光が必要なら、床や水底用の別メッシュに `M_Siliq_*_CausticsOverlay` を貼る

Hierarchyで対象を右クリックし、`Siliq Water > 完成水面を適用` から用途を選ぶだけでも同じ設定になります。

## 互換用ランタイム生成 API

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

`RuntimeWaterMapApplier` は既存シーンとの互換用APIです。新規シーンの標準導線にはせず、必要な場合だけコードから追加して使います。
起動時の停止を避けたい場合は `generateAsync` を ON にすると、色計算をバックグラウンドで行い、
同じ設定・解像度の生成結果は `useTextureCache` によりシーン内で使い回されます。

## 同梱シェーダー

### `Siliq/Water URP` (URP 用 / 任意 Sample)

SRP Batcher 対応・1 パス。URP が入っているプロジェクトだけで使う任意 Sample です。
Built-in / VRChat / Quest / iOS 向けの通常導入ではコンパイル対象にしないため、URP package が無いプロジェクトでも import error を起こしません。

導入:

`Package Manager > Siliq Water > Samples > URP Shader > Import`

- ノーマルマップ 2 レイヤースクロール、または**フローマップ駆動**の流れ(生成したフローマップをそのまま活用)
- リフレクションプローブによる映り込み + フレネル + スペキュラ
- **深度ベースの岸辺エフェクト**(浅瀬の色変化・岸辺フォームライン・水際の透明化) — URP 設定で Depth Texture を ON にして使用

### `Siliq/Water Mobile (Quest / iOS)` (ビルトイン RP 用)

1 パス・GrabPass なしのモバイル向け設計。通常は不透明、透明マテリアル作成時は
`_Opacity` / `_AlphaFresnel` / `_EdgeReflection` / `_SrcBlend` / `_DstBlend` / `_ZWrite` を
切り替えて iOS でも透ける水面にできます。

- ノーマルマップ 1 枚を 2 回スクロールサンプリング
- `_DisplacementStrength` による実頂点変位。ハイトマップがある場合は `_HeightMapInfluence` で混ぜられます
- 深い色 ⇔ 浅い色 + フレネル + 透過光 + 細い光の揺らぎ + スペキュラ + 任意のキューブマップ反射
- `_ReflectionPatternStrength` / `_ReflectionPatternScale` による空や窓の帯状反射
- `_RefractionStrength` による GrabPass なしの軽量な水越し揺らぎ
- `_CausticsMap` / `_CausticsStrength` / `_CausticsScale` / `_CausticsSpeed` / `_CausticsFocus` / `_CausticsPrismStrength` / `_CausticsScatterStrength` / `_BottomVisibility` / `_BottomLightStrength` / `_CausticsTint` による水底の光模様
- `_ReflStrength` はキューブマップ未使用時も反射量として効くため、反射が足りない時に直接上げられます
- `_MinLighting` / `_DarkReflectionDamping` / `_DarkDetailDamping` により、暗い部屋では反射ときらめきを減衰
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
通常の完成Materialメニューでは勝手に追加しません。環境演出として必要な水面だけに手動で追加してください。

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

1. Package Manager から `Siliq Water` を Remove
2. プロジェクトの `Packages/packages-lock.json` から古い `com.siliq.water-normalmap` の参照を更新、または削除して再解決
3. 必要なら `Library/PackageCache/com.siliq.water-normalmap*` を削除
4. Git URL を最新 commit で Add し直す

### `Core.hlsl` が見つからない

Built-in / VRChat プロジェクトに URP package が入っていない状態で `Siliq/Water URP` を直接 import すると発生します。通常導入では URP shader は読み込まれません。

- Built-in / VRChat / Quest / iOS: `Siliq/Water Mobile (Quest)` を使う
- URP: Universal Render Pipeline を導入した上で `Samples > URP Shader` を Import する

### Materialがピンクになる

ピンクは Unity がその shader を現在の Render Pipeline でコンパイル・表示できない時に出ます。
`2.3.88` 以降の完成Material画面と右クリック適用はRender Pipeline互換性を先に確認し、品質の異なるfallback Materialを自動生成しません。
Built-in / VRChatでは `Siliq/Water Mobile (Quest)` を使います。URPではUniversal Render Pipelineを導入した上で、Package Managerの `Samples > URP Shader` をImportしてください。

古い `Assets/SiliqWater/GeneratedMaterials/` は旧版が作ったMaterialです。最新の `PrebakedPack/ReadyMaterials/M_Siliq_*_Ready` へ差し替えてください。

### フラッグシップ水面が氷割れ・多角形模様に見える

古い `M_Water_Look_FlagshipCrystal` では height map の影響が強く、粗い Plane で大きな多角形や氷の割れ目のように見える場合があります。
最新 package に更新後、対象の水面を選択して次を実行してください。

`Tools > Siliq Water > かんたん作成 > 選択中の水面を診断して自動修復`

`2.3.30` 以降は、フラッグシップ透明水の初期値では height map を強く使わず、弱い手続きうねりと細波 normal、反射で水面を作ります。
実高さを上げる場合は、分割済み水面メッシュを使い、`ハイトマップの影響` を少しずつ上げてください。

### 波紋や波がピクピクする

`2.3.31` 以降では、モバイル向け shader の時間計算と波紋計算を高精度化し、波紋の出現/消滅を滑らかなフェードに変更しています。
古い波紋Materialを使っている場合は完成Materialへ差し替え、波紋が必要な水面だけに `WaterRippleEmitter` または `WaterRippleSource` を追加してください。
強くしたい場合も、まず `Ripple Amplitude` は 0.4-0.7、`Ripple Width` は 0.4 以上、`Ripple Lifetime` は 3.5 秒以上から調整してください。

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
  Textures/                 基本 normal 5 種 (1024px) + 用途別 normal/height/caustics (2048px)
  ReadyMaterials/           アタッチするだけで動く Siliq 水マテリアル 9 種 + 床用 caustics overlay 3 種
  Prefabs/                  CrystalLagoon / Hero / SunlitPool の完成セット
  Materials/                設定済み Standard マテリアル 6 種
  SampleScene/              SC_CrystalLagoon_Showcase.unity + 基本 5 種比較シーン
  Preview/                  Plane に貼った状態のプレビュー画像、Crystal Lagoon / Hero 完成形の確認用画像
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
