# 用途別マテリアルプリセットガイド

Siliq Water は、ノーマルマップ単体ではなく「用途別の見た目プリセット」として使うことを前提にする。まず完成形を確認する場合は `PrebakedPack/Preview/preview_crystal_lagoon_complete.png`、美しさ最優先なら `PrebakedPack/Preview/preview_crystal_lagoon_hero_complete.png` で方向性を見てから、Hero は `PrebakedPack/Prefabs/PF_Siliq_CrystalLagoon_Hero_Complete.prefab`、通常確認は `PrebakedPack/Prefabs/PF_Siliq_CrystalLagoon_Complete.prefab`、透明プール確認は `PrebakedPack/Prefabs/PF_Siliq_SunlitPool_Complete.prefab` を Hierarchy にドラッグする。プレビューは線模様の見本ではなく、室内プールの水面反射、薄い床、透明な水、水底光が見える完成イメージとして扱う。Prefab には水面、薄いタイル感のある明るいプール床、床用 caustics overlay、確認用ライトが一体で入っている。

アタッチだけで使う場合は `PrebakedPack/ReadyMaterials/` の `M_Siliq_*_Ready` を Renderer にドラッグする。これらは normal、色、透明度、反射、scroll が設定済みで、`WaterSurfaceAnimator` なしでも shader 側で波が動く。水底光は水面 material へ混ぜず、床や水底用の別メッシュに caustics overlay material を貼る。美しさを最優先して確認する場合は `M_Siliq_CrystalLagoon_Hero_Ready` から始める。ガラス水盤や水テーブルは `M_Siliq_WaterTable_Ready` を水面メッシュだけに貼り、ガラス天板、LED ライン、中央ノズル、ベースは別オブジェクトで作る。
Hero / Crystal Lagoon / 透明プールは、置いた瞬間に水面が滑って見えないよう shader scroll と Animator speed を低速にしつつ、止まって見えない値にしている。`Speed` は 0.3 を静かな水面の中間操作値として扱い、もっと静止に近づけたい場合は `Speed` を 0.05-0.12 へ下げ、完全に止めたい場合だけ `_Scroll1` / `_Scroll2` を 0 にする。高さの動きが速い場合は `_DisplacementSpeed` を下げる。透明水の平板さが気になる場合は `_RefractionStrength` を 0.25-0.45、`_DepthTintStrength` を 0.25-0.4 の範囲で使い、反射と奥行きを薄く歪ませる。暗い部屋で水面だけ浮く場合は WaterSurfaceAnimator の暗所項目で `_DarkReflectionDamping` / `_DarkDetailDamping` を上げる。
床に直接重ねる caustics overlay では `_Focus` で焦点線、`_SoftScatter` で柔らかい光膜、`_PrismStrength` で薄い色分散を調整する。Hero は通常版より少し高い値から始める。水底、プール床、水槽底は水面とは別のメッシュとして扱う。

水面オブジェクトを選択して作成・再適用したい場合は、Hierarchy 右クリックから以下を適用する。

`Siliq Water > 完成水面を適用`

最高品質確認は `最高品質 クリスタルラグーン Hero` を選ぶ。`Tools > Siliq Water > 完成マテリアル` ではPC / Quest / iOSと用途を選び、同じ完成Materialを選択中の水面へ直接設定する。通常操作ではTextureやMaterialを自動生成しない。

Unity / VRChat 初心者は、まず `Tools > Siliq Water > はじめてガイド` または `Tools > Siliq Water > 完成マテリアル` を開く。そこから Hero 完成セット配置、透明プール完成セット配置、Hero 水面作成、WaterTable 水面作成、既存水面の診断修復、README / 用途別ガイド / PrebakedPack の確認に進める。見た目を最優先で確認する場合は `Tools > Siliq Water > かんたん作成 > 最高品質 Hero 完成セットを配置` を使う。プール用途なら `透明プール完成セットを配置` を使う。水面、明るい床、caustics overlay、確認用ライトが一括で置かれる。水テーブル用途は `ウォーターテーブル水面を作成` を使う。水面だけを作る場合は `最高品質 Hero 水面を作成`、既存の水面がピンク、動かない、高さが出ない場合は `選択中の水面を診断して自動修復` を実行する。

## プリセット

| プリセット | 用途 | 見た目 |
| --- | --- | --- |
| 美しい海 (Clear Sea) | 綺麗な海、リゾート水面、広い透明水 | 青緑の透明感、強めのフレネル反射、細いきらめき |
| 透明プール (Clear Pool) | プール、浅い水、建築ビジュアル | 高い透明度、弱い凹凸、控えめな光網 |
| 室内ブループール (Indoor Blue Pool) | 明るい室内プール、窓反射、アニメ調の青い水面 | 青い透過、広い白ハイライト、強めの反射、控えめな高さ |
| ウォーターテーブル (Water Table) | ガラス水盤、LED 内蔵テーブル、中央オブジェクト付きの浅い水面 | 黒青い浅い水、中央リング波、強いエッジ反射 |
| フラッグシップ透明水 (Flagship Crystal) | 製品デモ、メインビジュアル、最上位の透明水 | 厚めの透明感、強い鏡面反射、多層細波、実高さ |
| クリスタルラグーン (Crystal Lagoon) | 透き通った美しさを最優先した浅い水、プール、リゾート水面 | 淡い青緑、強い透過光、控えめな凹凸、別メッシュの水底光 |
| クリスタルラグーン Hero ReadyMaterial | 1000点の見栄え確認、商品ページ、透明水の最初の基準 | さらに高い透明な抜け感、強い反射帯、低い凹凸、別メッシュの Hero 水底光 |
| 血の海 (Blood Sea) | ホラー、異世界、赤い液体 | 深い赤、重い粘度感、弱い反射 |
| 液体金属 (Liquid Metal) | 水銀、金属液、SF 表現 | 高反射、高スムースネス、不透明で鏡面寄り |

## 自動処理

プリセット適用時に以下をまとめて行う。

- 用途ごとに手調整済みの `M_Siliq_*_Ready` をPackageから直接割り当てる
- フラッグシップ透明水では専用 2048px normal map と height map を割り当てる
- クリスタルラグーンでは水面に専用 2048px normal map、height map を割り当て、`Water_Caustics_CrystalLagoon_01.png` は床/水底用 caustics overlay で使う
- `M_Siliq_CrystalLagoon_Hero_Ready` は `Water_Normal_CrystalLagoon_Hero_01.png`、`Water_Height_CrystalLagoon_Hero_01.png` を使い、通常 Crystal Lagoon texture の流用に戻さない。水面側の `Bottom Visibility`、`Bottom Light Strength`、`Caustics Strength` は初期値 0 にし、`Water_Caustics_CrystalLagoon_Hero_01.png` は `M_Siliq_CrystalLagoon_Hero_CausticsOverlay` で使う
- `M_Siliq_WaterTable_Ready` は `Water_Normal_WaterTable_01.png`、`Water_Height_WaterTable_01.png` を使い、中央から広がる浅いリング波を水面側に持つ。水底 caustics、飛沫、泡、粒子は入れない
- 完成形をすぐ確認できるよう、`PF_Siliq_CrystalLagoon_Complete.prefab` には水面、`Siliq/Pale Pool Floor Mobile` の薄いタイル床、`M_Siliq_CrystalLagoon_CausticsOverlay` の床用光を同梱する。`PF_Siliq_CrystalLagoon_Hero_Complete.prefab` では Hero water material と `M_Siliq_CrystalLagoon_Hero_CausticsOverlay` を使う。`PF_Siliq_SunlitPool_Complete.prefab` では ClearPool material と `M_Siliq_SunlitPool_CausticsOverlay` を使う
- 床に直接光を出したい場合は、床の少し上に薄い Plane を置いて用途に合う caustics overlay material を貼る
- 透明プール / 室内ブループールでは `Water_Caustics_SunlitPool_01.png` を `M_Siliq_SunlitPool_CausticsOverlay` で床に重ねる。広い床光と柔らかい光リボンから始める
- 透明な海・フラッグシップ水で水底光が必要な場合も、水面 material ではなく別メッシュ/別 material 側に割り当てる
- 色、透明度、反射、透過光、きらめきを設定する
- `Clarity`、`Reflection Pattern`、`Refraction`、`Depth Tint` を用途ごとに設定し、Crystal Lagoon では水色の濁りを抑え、空や窓の帯状反射が見える状態から始める
- `WaterSurfaceAnimator` を追加または更新し、動きを付ける
- Built-in / iOS 系では `Siliq/Water Mobile (Quest)` を使う
- Built-in / VRChat / Quest / iOSでは `Siliq/Water Mobile (Quest)` の完成Materialを使う
- URPではUniversal Render Pipeline導入後に `URP Shader` SampleをImportする。Built-in完成Materialから品質の異なるfallbackを自動生成しない

## 調整の目安

- 綺麗な海: `Normal Strength` を 0.8-1.1、`Opacity` を 0.5-0.7
- プール: `Normal Strength` を 0.25-0.5、`Opacity` を 0.38-0.55
- 室内ブループール: `Normal Strength` を 0.35-0.6、`Opacity` を 0.35-0.5、`Reflection` は高め
- ウォーターテーブル: `Normal Strength` は 0.4 前後、`Opacity` は 0.5 前後、`Reflection Pattern` は 0.6 以上、`Height Map Influence` は 0.15-0.25、`Height` は 0.008 以下から始める。中央リングを崩さないため `_Tiling2` は 1.0-1.15 に抑える
- フラッグシップ透明水: `Normal Strength` を 0.55-0.65、`Opacity` を 0.48-0.62、`Reflection` は高め、`Height` は 0.02 以下から始める
- クリスタルラグーン: `Normal Strength` は 0.4 前後、`Clarity` は 0.9 以上、`Reflection Pattern` は 0.42 以上、`Transmission` は高め、`Height` は 0.01 以下に抑える。水面側の `Caustics` / `Bottom` 系は 0 のままにする
- クリスタルラグーン Hero: `Normal Strength` は 0.36 前後、`Opacity` は 0.34 前後、`Clarity` は 0.98、`Reflection Pattern` は 0.58、`Transmission` は 0.98 を基準にする。水底光は Hero caustics overlay 側で調整する
- 水底の光: プールや浅い海では水底/床の少し上に別メッシュを置き、caustics overlay material の `Intensity` / `Focus` / `Soft Scatter` を調整する
- 床用 caustics overlay: `Intensity` は 0.55-0.8、`Tiling` は 1-2 から始める。床が発光しすぎる場合は `Floor Fade` を下げる。
- 氷割れや多角形模様に見える場合: フラッグシップ透明水の `Height Map Influence` は 0 付近、`Height` は 0.02 以下から始める
- 血の海: `Opacity` を 0.6 以上、`Reflection` は控えめ
- 液体金属: `Opacity` は 1、`Refl Strength` と `Smoothness` は高め

## 注意

- 透明感は normal map だけでは作れない。必ず用途別プリセットか透明マテリアルを使う。
- 高品質に見せるには normal map、色、透明度、反射、ハイライト、実高さ、別メッシュの水底光、ライト、分割メッシュの全部が必要。
- フラッグシップ透明水は Calm の流用ではなく、`Water_Normal_FlagshipCrystal_01.png` と `Water_Height_FlagshipCrystal_01.png` を前提にする。
- クリスタルラグーンは Flagship の流用ではなく、`Water_Normal_CrystalLagoon_01.png`、`Water_Height_CrystalLagoon_01.png` を水面に使う。Hero では `Water_Normal_CrystalLagoon_Hero_01.png`、`Water_Height_CrystalLagoon_Hero_01.png` を使う。
- ウォーターテーブルは `Water_Normal_WaterTable_01.png`、`Water_Height_WaterTable_01.png` を水面に使う。ガラス端の発光、LED、ベース、中央オブジェクトは別メッシュ/別 material で作る。
- `Water_Caustics_CrystalLagoon_01.png`、`Water_Caustics_CrystalLagoon_Hero_01.png`、`Water_Caustics_SunlitPool_01.png` は水面 material ではなく床用/水底用の `M_Siliq_*_CausticsOverlay` で使う。
- `M_Siliq_CrystalLagoon_CausticsOverlay`、`M_Siliq_CrystalLagoon_Hero_CausticsOverlay`、`M_Siliq_SunlitPool_CausticsOverlay` は水面 material ではなく床用の加算 overlay。水面には用途別の `M_Siliq_*_Ready` を使う。
- 血や金属液体は「水」ではなく特殊液体なので、色だけでなく反射と凹凸の強さを変える。
- 液体金属は透過させず、不透明で反射を強くした方が破綻しにくい。
