# Changelog

## [2.3.28] - 2026-07-08

### 改善: フラッグシップ水面を専用ベイクへ昇格
- `PrebakedPack` に `Water_Normal_FlagshipCrystal_01.png` と `Water_Height_FlagshipCrystal_01.png` を追加し、フラッグシップ透明水だけ 2048px の専用 normal / height を同梱
- `M_Water_FlagshipCrystal` を追加し、Calm normal の流用ではなく専用 normal と height を参照するようにした
- 用途別 `フラッグシップ透明水 (Flagship Crystal)` の Quick Apply が専用 normal / height を Siliq 水シェーダーへ割り当て、実高さにも使うようにした
- 専用アセットの解像度、NormalMap import、Quick Apply の割り当てが退化しないようテストを追加

## [2.3.27] - 2026-07-08

### 追加: Unity / VRChat 初心者向けのかんたん作成と診断
- Normal map 生成を中心差分から Sobel 勾配へ変更し、細かい凹凸の硬さやジャギー感を抑えるようにした
- `Tools > Siliq Water > かんたん作成 > フラッグシップ水面を作成` を追加し、分割済み水面メッシュ、フラッグシップ透明水マテリアル、`WaterSurfaceAnimator`、最低限のライト/カメラを一括作成できるようにした
- `Tools > Siliq Water > かんたん作成 > 選択中の水面を診断して自動修復` を追加し、未対応 shader、マテリアル未設定、Animator なし、頂点不足の Plane / Quad を自動修復できるようにした
- Unity 標準 Plane の Default Material のように shader は有効でも水ではない状態を検出し、フラッグシップ水マテリアルへ差し替えるようにした
- 実高さが見えない原因になりやすい 1 枚 Quad を、96 分割の水面グリッドへ交換する初心者向け修復を追加
- 初心者用メッシュ生成と自動修復のテストを追加

## [2.3.26] - 2026-07-08

### 追加: フラッグシップ透明水プリセット
- 水面マッププリセットに `フラッグシップ透明水 (多層反射)` を追加し、製品デモ向けの広い鏡面うねり、細波、淡いコースティクス、低ラフネスの生成レシピを追加
- `Siliq Water > 用途別マテリアルを適用` に `フラッグシップ透明水 (Flagship Crystal)` を追加
- 水面マップスタジオの `目的から始める` に `フラッグシップ透明水` を追加し、4096px、48 フレーム、Height / Flow / DUDV / Caustics 出力、透明 Siliq マテリアルを一括設定できるようにした
- 自動作成マテリアルでフラッグシップ用の高反射、厚めの透明感、強い白ハイライト、実高さを設定するようにした
- フラッグシップ透明水が単調な一枚ノイズや太い白模様へ戻らないよう、専用テストを追加

## [2.3.25] - 2026-07-07

### 追加: 室内ブループールの水面プリセット
- 水面マッププリセットに `室内ブループール (窓反射)` を追加し、室内の窓反射が揺れるような広い低周波の水面を生成できるようにした
- `Siliq Water > 用途別マテリアルを適用` に `室内ブループール (Indoor Blue Pool)` を追加
- 水面マップスタジオの `目的から始める` に `室内ブループール` を追加し、透明 Siliq マテリアル、Height / DUDV / Caustics 出力、反射強めの青い初期値をまとめて適用できるようにした
- 自動作成マテリアルで室内ブループール用の青い透過、強めの窓反射、柔らかい白ハイライト、控えめな高さを設定するようにした

## [2.3.24] - 2026-07-07

### 改善: 水面の実高さを追加し、板っぽさを軽減
- `Siliq/Water Mobile (Quest)` に `_DisplacementStrength` / `_DisplacementScale` / `_DisplacementSpeed` / `_HeightMapInfluence` / `_HeightMap` を追加
- ノーマルだけでなく頂点を上下させる実ジオメトリ変位を追加し、Plane 上でも水面が起伏するようにした
- `WaterSurfaceAnimator` と専用 Inspector に高さ / 波長 / 高さ速度 / ハイトマップ影響を追加
- 水面マップスタジオの目的別プリセットで Height map も書き出し対象にし、Siliq material へ自動割り当てするようにした
- 用途別マテリアルにも海・プール・血の海・液体金属ごとの高さ初期値を追加

## [2.3.23] - 2026-07-06

### 改善: 水の色・透明感・反射を Inspector から直接調整可能に
- `WaterSurfaceAnimator` に明るい水色 / 深い水色 / 反射色 / 透過光 / きらめき色を追加し、MaterialPropertyBlock で即反映できるようにした
- 透過光量とハイライト量も Animator から調整できるようにし、透明感・反射・色合わせを一箇所で操作可能にした
- `Siliq/Water Mobile (Quest)` の `_ReflStrength` がキューブマップ未使用時にも反射量として効くよう修正
- 透明水と用途別プリセットの初期値を、反射・透過光・ハイライトが見える方向へ調整
- `WaterSurfaceAnimator` 専用 Inspector を追加し、色 / 透明・反射 / 動きを分けて操作できるようにした

## [2.3.22] - 2026-07-06

### 修正: URP / Built-in の不一致でマテリアルがピンクになる経路を遮断
- Tool / Quick Apply の shader 選択を Render Pipeline 対応で判定し、URP では Built-in 用 `Standard` / `Siliq/Water Mobile (Quest)` を自動適用しないようにした
- URP で `Siliq/Water URP` sample が未導入または使えない場合は `Universal Render Pipeline/Lit` へ fallback し、ピンク material を作らないようにした
- 通常の右クリック適用でも Prebaked の Standard material をそのまま貼らず、現在の Render Pipeline に合う generated material へ変換するようにした
- 既存の generated material も再適用時に安全な shader へ上書きされるようにした

## [2.3.21] - 2026-07-06

### 修正: 暗い場所で水面だけ銀色に浮く見え方を抑制
- `Siliq/Water Mobile (Quest)` に `_MinLighting` / `_DarkReflectionDamping` / `_DarkDetailDamping` を追加
- ambient が暗い場所では空反射、細いきらめき、glint、ripple の明るさを落とし、黒背景で水面だけ白く浮かないようにした
- Quick Apply と水面マップスタジオで生成する Siliq 水マテリアルにも暗所向けの初期値を保存するようにした

## [2.3.20] - 2026-07-06

### 修正: Tool から作成したマテリアルがピンクになる問題を回避
- 水面マップスタジオの自動マテリアル作成で、選択中 shader が見つからない、または `isSupported == false` の場合は使用しないよう変更
- `Siliq/Water Mobile (Quest)` や `Siliq/Water URP` が環境に合わない場合、ピンク material を作らず URP Lit / Standard へ自動 fallback するようにした
- 以前の EditorPrefs で使えない shader が選択されたままでも、起動時に安全な shader へ補正するようにした
- Quick Apply 側も unsupported shader を material に設定しないようにし、ピンク化を避けるようにした

## [2.3.19] - 2026-07-06

### 修正: Edit Mode 操作中の CPU / GPU 負荷を低減
- `WaterSurfaceAnimator` の Edit Mode プレビューを、選択中の水面だけ更新するよう変更
- Edit Mode の連続プレビューを最大 10fps に制限し、未選択の水面が裏で Scene View を再描画し続けないようにした
- 水面マップスタジオのアニメプレビューを最大 12fps に制限し、スライダー操作中のプレビュー再生成も間引くようにした
- `Speed` が 0 の時は Edit Mode 更新を行わず、調整中の発熱とファン回転を抑えるようにした

## [2.3.18] - 2026-07-06

### 改善: Animator から透明感と反射を直接調整できるように変更
- `WaterSurfaceAnimator` に `Opacity` / `Edge Reflection` / `Reflection Strength` /
  `Sparkle` を追加し、Inspector から水面の不透明度、輪郭反射、反射量、きらめきを調整できるようにした
- 透明・反射系の値は `MaterialPropertyBlock` で反映し、共有マテリアルを直接汚さない
- 右クリック適用時に現在のマテリアル値から Animator の透明・反射スライダーを初期化するようにした
- `Speed` の範囲を `0..0.6` に変更し、`0.3` が標準の中間値になるよう各プリセット速度を再調整

## [2.3.17] - 2026-07-06

### 改善: PC 透明水の初期見た目を濃く、失敗しにくく調整
- `透明な水マテリアルを適用 (PC)` は、可能な場合 Standard Transparent ではなく
  `Siliq/Water Mobile (Quest)` を使い、Fresnel、反射、透過光、細いきらめき込みの水面として生成するよう変更
- 既存の `M_Water_*_Transparent` も再適用時に Siliq 水シェーダーへ更新され、Animator の対象も `_NormalMap` に切り替わる
- PC / iOS 透明、用途別の `美しい海` / `透明プール` の初期 `Opacity` を上げ、
  白い Scene View や一枚板の検証でも透明すぎる見え方になりにくくした
- 水面マップスタジオのスライダー表示を `透明度` から `不透明度` に変更し、
  1 に近いほど濃いという挙動が分かるようにした

## [2.3.16] - 2026-07-06

### 修正: Built-in / VRChat プロジェクトでの URP シェーダー import error
- `Siliq/Water URP` を通常 Runtime から外し、Package Manager の任意 Sample として導入する構成に変更
- Universal Render Pipeline が入っていないプロジェクトでは URP include をコンパイル対象にしないようにし、
  `Couldn't open include file 'Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl'` を回避
- 水面マップスタジオと Quick Apply は、URP シェーダーが実際に存在する場合のみ `Siliq/Water URP` を選び、
  それ以外では `Siliq/Water Mobile (Quest)` へフォールバック
- `package.json.meta` 警告が Unity の古い PackageCache / `packages-lock.json` 由来で残る場合の対処を README に追記

## [2.3.15] - 2026-07-06

### 追加: 公開リポジトリ向けの製品品質ファイル
- `.gitignore` / `.gitattributes` を追加し、Unity 生成物、IDE ファイル、バイナリアセットの扱いを整理
- `LICENSE.md` / `SECURITY.md` / `CONTRIBUTING.md` を追加し、公開時の権利表記、脆弱性報告、変更ルールを明文化
- `Docs/PublicReleaseChecklist.md` を追加し、public 化・リリース前に確認する項目を整理
- `package.json` に license / repository / documentation / changelog / license URL を追加

## [2.3.14] - 2026-07-06

### 改善: 目的ベースの高品質ワークフローを追加
- 水面マップスタジオに `目的から始める` を追加し、綺麗な海 / 透明プール /
  血の海 / 液体金属の用途を選ぶだけで、生成レシピ、品質、出力マップ、
  マテリアル作成設定を推奨値へ一括変更できるようにした
- `軽量` / `高品質` / `最高品質 EXR` の品質ショートカットを追加し、
  解像度、スーパーサンプリング、EXR 書き出しをワンクリックで切り替えられるようにした
- 共通 Editor UI 仕様にも目的別入口と品質ショートカットのルールを追加

## [2.3.13] - 2026-07-06

### 追加: 用途別マテリアルプリセット
- `Siliq Water > 用途別マテリアルを適用` に `美しい海` / `透明プール` /
  `血の海` / `液体金属` を追加し、ノーマル、色、透明度、反射、動きをまとめて適用できるようにした
- URP プロジェクトでは `Siliq/Water URP`、Built-in / iOS 系では
  `Siliq/Water Mobile (Quest)` を優先して用途別マテリアルを生成
- `Docs/MaterialLookPresetGuide.md` を追加し、特殊液体を含む見た目プリセットの使い分けを整理

## [2.3.12] - 2026-07-06

### 追加: 共通 Editor ツール UI 仕様書
- 水面マップスタジオの左操作 / 右固定プレビュー構成を、他の Editor ツールにも応用できる共通仕様として `Docs/CommonEditorToolLayoutSpec.md` に整理
- 2 ペイン切替幅、左ペイン幅、セクション順、プレビュー挙動、移植チェックリストを明文化

## [2.3.11] - 2026-07-06

### 改善: 水面マップスタジオの操作性を向上
- 広いウィンドウでは左側を操作パネル、右側を固定プレビューに分離し、
  パラメータ調整中もプレビューが画面外へ流れないように変更
- 操作エリアを `プリセット` / `全体設定` / `波レイヤー` / `書き出し` の順に整理し、
  プレビューのマップ切り替えもサイドパネル幅に合わせて見やすく調整

## [2.3.10] - 2026-07-06

### 修正: PC 透明マテリアルのアニメーションと package meta 警告
- `WaterSurfaceAnimator` が Standard / URP Lit の normal map でも見た目が動くよう、
  `_BumpMap_ST` に加えて `_MainTex_ST` / `_BaseMap_ST` も更新するように修正
- `package.json.meta` を同梱し、Git / UPM で immutable package として読み込んだ時の
  `package.json has no meta file` 警告を解消

## [2.3.9] - 2026-07-06

### 改善: Cyber プリセットを実用的な SF 水面へ変更
- `サイバー (細いデータ流)` の生成レシピを、太い横縦グリッド、規則ドット、
  セル境界の網目ではなく、細いデータ流、斜めスキャン光、微細なホログラム揺らぎで構成する方向へ変更
- 同梱 `Water_Normal_Cyber_01.png` を新レシピで再生成し、
  `M_Water_Cyber` の `_BumpScale` と Quick Apply の Cyber 初期強度を下げた
- Cyber を床タイル状の模様ではなく、近未来 / ホログラム系の透明水面として使える方向へ調整

## [2.3.8] - 2026-07-06

### 修正: Pool プリセットの強い法線表現を調整
- `プール (光の網目)` の生成レシピを、太いリボン状の凹凸になりにくい
  浅く細い光網 + 控えめな水面揺らぎへ変更
- 同梱 `Water_Normal_Pool_01.png` を新レシピで再生成し、
  `M_Water_Pool` の `_BumpScale` と Quick Apply の Pool 初期強度を下げた
- Pool は「凹凸を強く盛る水面」ではなく、浅い水の細い光ゆらぎとして扱うように調整

## [2.3.7] - 2026-07-06

### 改善: 大きな水面の均一さを低減
- `Siliq/Water Mobile (Quest)` と `Siliq/Water URP` に
  `_MacroVariation` / `_MacroScale` / `_MacroDirectionBreakup` /
  `_MacroColorVariation` を追加。ワールド空間の低周波ムラで、
  UV、方向、法線強度、色、反射/ハイライトを場所ごとに変えるようにした
- 右クリック適用と水面マップスタジオの Siliq 系マテリアル自動作成で、
  マクロムラを初期値として設定。大きい面に貼った時の「全面が同じ密度」
  に見える問題を軽減

## [2.3.6] - 2026-07-06

### 修正: 波紋プリセットを「横に流す」表現から「外へ広がる」表現へ変更
- 右クリックの `波紋 (Ripple)` プリセットを、焼き済みノーマルの UV スクロールではなく、
  発生点から同心円状に広がる procedural ripple に変更
- `WaterRippleEmitter` を追加。Renderer 範囲内に波紋発生点を自動で作り、
  Play 中にリングが外側へ広がる雨面/環境波紋を作れるようにした
- `Siliq/Water Mobile (Quest)` と `Siliq/Water URP` の波紋計算を、
  法線変化だけでなくリングの光も返す形へ調整し、広がりが視覚的に分かりやすくなるよう改善

## [2.3.5] - 2026-07-05

### 改善: iOS/Mobile 透明水の高品質化
- `Siliq/Water Mobile (Quest)` の透明表現に透過光、細い光の揺らぎ、
  角度依存のきらめきを追加。alpha だけで薄くする見え方から、
  水面内部の光と輪郭反射で透明感を作る方向へ改善
- iOS/Mobile 透明プリセットのデフォルトを、正面はより透け、
  斜め視線では Fresnel alpha と反射が戻る設定に再調整
- Quick Apply と水面マップスタジオの自動作成マテリアルで、
  新しい透過光・グリマー・グリント設定を同じ値で適用するよう統一

## [2.3.4] - 2026-07-05

### 改善: 透明水の品質向上
- `Siliq/Water Mobile (Quest)` の透明時の見え方を固定 alpha から
  Fresnel 連動 alpha へ変更。正面は透け、斜め視線では反射と不透明感が増す
  水らしい挙動にした
- `_AlphaFresnel` / `_AlphaPower` / `_EdgeReflection` を追加し、
  iOS/Mobile 透明マテリアル作成時に反射・スペキュラ・透明度を水向けに調整
- iOS/Mobile 透明プリセットの色を、薄い板に見えにくい浅瀬色/深水色/空反射色へ再調整

## [2.3.3] - 2026-07-05

### 追加: iOS / Mobile 透明水
- `Siliq/Water Mobile (Quest)` に `_Opacity` と Blend / ZWrite 用プロパティを追加。
  デフォルトは従来通り不透明、透明マテリアル作成時だけ alpha blend に切り替わる
- 右クリックメニューに `透明な水マテリアルを適用 (iOS/Mobile)` を追加。
  Prebaked ノーマルを `Siliq/Water Mobile (Quest)` の `_NormalMap` に割り当て、
  iOS 向けにも軽い透明水として使えるマテリアルを生成する
- 水面マップスタジオで `Siliq/Water Mobile (Quest / iOS)` を選び、
  `透明マテリアルとして作成` を ON にした場合も `_Opacity` と alpha blend を設定する

## [2.3.2] - 2026-07-05

### 追加: PC 向け透明マテリアル
- 右クリックメニューに `透明な水マテリアルを適用 (PC)` を追加。
  Prebaked のノーマルマップを使いながら、Standard Transparent 設定済みの
  マテリアルを `Assets/SiliqWater/GeneratedMaterials/` に生成して適用する
- 水面マップスタジオの自動作成マテリアルに
  `透明マテリアルとして作成` と `透明度` を追加。
  Standard / URP Lit は Transparent Blend、Siliq URP は `_Opacity` に反映する
- README に「ノーマルマップだけでは透明感は出ず、マテリアル Blend / Alpha が必要」
  であることを明記

## [2.3.1] - 2026-07-05

### 修正: 水面アニメーションの反映を確実化
- `WaterSurfaceAnimator` を `MaterialPropertyBlock` ベースに変更し、
  共有マテリアルを汚さずに `_BumpMap_ST` / `_BumpScale` を更新するよう修正
- 同梱 Siliq 水シェーダーでは `_Scroll1` / `_Scroll2` / `_NormalStrength` /
  `_Tiling*` を直接制御し、`_NormalMap` の `[NoScaleOffset]` でも
  「動き」「見た目」の調整が効くようにした
- 右クリック適用時の初期速度・強さ・タイリングを見えやすい値へ調整

### 改善
- ランタイム生成に非同期生成とテクスチャキャッシュを追加
- 波紋の接触位置を水面 Y へ投影する方式に変更し、複数水面向けの
  `_RippleChannel` を追加
- 書き出し処理の import をまとめ、アトラス import サイズとメモリ上限チェックを修正
- 自動作成マテリアルで Siliq URP の Flow / Foam マップを割り当てるよう改善
- QuickApply の Prebaked マテリアル検索に GUID 以外の fallback を追加
- `WaterSurfaceAnimator` の PropertyBlock 反映を EditMode テストで検証

## [2.3.0] - 2026-07-05

### 追加: iOS 対応
- PrebakedPack のテクスチャ、および水面マップスタジオの書き出しに
  iOS (iPhone) 向けインポート設定 (ASTC 6x6, 最大 1024px) を追加。
  Android と同様に自動適用される
- 同梱シェーダーは標準的な CG/HLSL のみで書かれており Metal (iOS) でも動作する旨を明記

### 追加: 触れたら波紋が広がるインタラクティブな水面
- `Siliq/Water Mobile (Quest)` `Siliq/Water URP` 両シェーダーに
  「触れた時の波紋を有効化」(`_USE_RIPPLES`) を追加。ワールド空間座標を中心に
  実時間で広がる波紋 (速さ/幅/持続時間/強さを調整可能) をノーマルに合成する
- `Runtime/Components/WaterRippleSource.cs` を追加。Trigger コライダーに
  アバター等が接触するとその位置から波紋を発生させる (エディタ/一般 Unity アプリ向け)
- `Runtime/VRChatSupport/WaterRippleSourceUdon.cs.txt` を追加。VRChat ワールドで
  全プレイヤーの接触を反映する UdonSharp 版のソース (要 VRChat SDK3 + UdonSharp、
  コンパイル事故を避けるため意図的に `.cs.txt` として同梱。**未検証、要動作確認**)

## [2.2.3] - 2026-07-05

### 改善: WaterSurfaceAnimator を調整しやすく
- パラメータを生の Vector2 (`scrollSpeed`) から、直感的な
  **方向 (度, 0-360) / 速さ / 強さ / 模様の大きさ** の4スライダーに変更
- 強さは `_BumpScale` (Standard 等) に、模様の大きさはテクスチャスケールに反映
- `[ExecuteAlways]` 化し、**Play せずにシーンビュー上でその場にプレビュー**できるように変更
  (値を変えると即座に波の動きに反映される)
- WaterPackQuickApply (右クリック適用) も新パラメータに合わせて更新、
  水の種類ごとに方向・速さのプリセットを設定

## [2.2.2] - 2026-07-05

### 修正: パッケージが空に見える重大バグ
- git URL 経由でインストールした場合 (immutable package)、`.meta` ファイルが
  事前にコミットされていない全アセットが Unity に無視される問題を修正。
  PrebakedPack のテクスチャ/マテリアル以外、スクリプト・シェーダー・asmdef・
  シーン・README 等ほぼ全ファイルに `.meta` が欠落しており、パッケージの
  中身が丸ごと見えなくなっていた。不足していた 32 件の `.meta` を追加。

## [2.2.1] - 2026-07-05

### 追加: 水面を動かす WaterSurfaceAnimator
- `Runtime/Components/WaterSurfaceAnimator.cs` を追加。指定したテクスチャ
  プロパティ (`_BumpMap` 等) を時間経過でスクロールさせる軽量コンポーネント。
  Standard / URP Lit / VRChat Mobile など任意のシェーダーで動作する
- PrebakedPack のマテリアルは Standard シェーダーで元々 UV アニメーションを
  持たず静止画のままだったため、**右クリック一発適用時に自動で付与**するよう変更
  (水の種類ごとに異なるスクロール速度をプリセット)

## [2.2.0] - 2026-07-05

### 追加: 焼き済みパック (PrebakedPack) — ツール不要ですぐ使える
- 水ノーマルマップ 5 種を同梱: Calm / Ripple / Stream / Pool / Cyber
  (1024×1024 PNG、シームレス、2× スーパーサンプリング生成、
   NormalMap / Repeat / Android=ASTC 6x6 のインポート設定 .meta 付き)
- 設定済み Standard マテリアル 5 種 (`M_Water_*`) を同梱
- 5 種を並べて比較できるサンプルシーン `SC_WaterNormalMap_Preview.unity` を同梱
- Plane に貼った見た目が分かるプレビュー画像 5 枚を同梱
- **右クリック一発適用**: Hierarchy の `Siliq Water > 水マテリアルを適用` メニューで
  選択オブジェクトに水マテリアルを即適用 (Undo 対応)
- ツールに「プール (光の網目)」「サイバー (人工水面)」プリセットを追加
  (パック画像と同一レシピ — シード変更でバリエーション自作可能)
- README にパックの使い方・手動インポート設定・改変方法・透明化の注意を追記

## [2.1.0] - 2026-07-04

### 品質向上
- **16bit EXR 書き出し**を追加。穏やかな水面で発生する 8bit PNG のバンディング (縞) を根絶できます
- **スーパーサンプリング (2×)** を追加。ボロノイや尖った波のエッジのジャギーを低減します
- フォームマスクに**ぼかし回数** (ラップあり 3×3 ボックスブラー) を追加
- ランタイムベイク API に **highPrecision** オプション (RGBAHalf) を追加
- コアパイプラインを float 精度 (`Color[]`) に統一し、量子化は書き出しの最終段のみで実施

### 修正
- コースティクスをフレーム毎の正規化から**決定論的マッピング**に変更し、
  アニメーション書き出し時のちらつきを解消 (新パラメータ「コースティクスの強さ」で調整)
- 連番書き出し中に例外が発生した場合に `AssetDatabase.StartAssetEditing` が
  解除されずエディタが固まる問題を修正
- ランタイムベイクで全マップを linear テクスチャとして生成するように修正
- ウィンドウ再読込時に書き出しマップ選択が壊れる可能性を修正

### 追加
- **Unity Test Runner (EditMode) 用の自動テスト**を同梱。全プリセットの
  シームレスタイリング・完全ループ・値域・決定性を Unity 内で検証できます
- 両シェーダーに GPU インスタンシング対応 (`multi_compile_instancing`) を追加

## [2.0.0] - 2026-07-04

- 生成マップを 7 種へ拡張 (ノーマル / ハイト / フォーム / ラフネス / フロー / DUDV / コースティクス)
- コアを Runtime アセンブリ化し、ランタイム生成 API を追加
- ドメインワープ・マスクむら・64 波スペクトル合成を追加
- ScriptableObject プロファイル / JSON 共有 / マテリアル自動作成
- URP 水シェーダー (フローマップ駆動・岸辺エフェクト) を追加
- プリセット 8 種 (外洋 / 溶岩を追加)

## [1.0.0] - 2026-07-04

- 初回リリース: 水面ノーマルマップ生成ウィンドウ、6 種の波レイヤー、
  シームレスタイリング / 完全ループ保証、Quest 向けインポート設定自動適用、
  ビルトイン RP 向け軽量水シェーダー
