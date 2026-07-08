# 用途別マテリアルプリセットガイド

Siliq Water は、ノーマルマップ単体ではなく「用途別の見た目プリセット」として使うことを前提にする。まず完成形を確認する場合は `PrebakedPack/Preview/preview_crystal_lagoon_complete.png`、美しさ最優先なら `PrebakedPack/Preview/preview_crystal_lagoon_hero_complete.png` で方向性を見てから、Hero は `PrebakedPack/Prefabs/PF_Siliq_CrystalLagoon_Hero_Complete.prefab`、通常確認は `PrebakedPack/Prefabs/PF_Siliq_CrystalLagoon_Complete.prefab` を Hierarchy にドラッグする。水面、薄いタイル感のある明るいプール床、床用 caustics overlay、確認用ライトが一体で入っている。

アタッチだけで使う場合は `PrebakedPack/ReadyMaterials/` の `M_Siliq_*_Ready` を Renderer にドラッグする。これらは normal、色、透明度、反射、scroll、水底の光が設定済みで、`WaterSurfaceAnimator` なしでも shader 側で波が動く。美しさを最優先して確認する場合は `M_Siliq_CrystalLagoon_Hero_Ready` から始める。
Hero / Crystal Lagoon / 透明プールは、置いた瞬間に水面が滑って見えないよう shader scroll と Animator speed をかなり低速にしている。もっと静止に近づけたい場合は `Speed` を 0.01 以下、または material の `_Scroll1` / `_Scroll2` を 0 にする。Hero の水底光は直線格子ではなく、有機的に曲がる caustics の光筋として調整する。透明水の平板さが気になる場合は `_RefractionStrength` を 0.25-0.45 の範囲で使い、水底光と反射を薄く歪ませる。

水面オブジェクトを選択して作成・再適用したい場合は、Hierarchy 右クリックから以下を適用する。

`Siliq Water > 用途別マテリアルを適用`

最高品質確認は `クリスタルラグーン Hero (Crystal Lagoon Hero)` を選ぶ。水面マップスタジオでは `目的から始める` のボタンから同じ用途を選べる。ここでは生成レシピ、解像度、スーパーサンプリング、出力マップ、透明マテリアル作成までまとめて推奨値に変わる。

Unity / VRChat 初心者は、まず `Tools > Siliq Water > はじめてガイド` を開く。そこから完成 Prefab の選択、Hero 水面作成、既存水面の診断修復、README / 用途別ガイド / PrebakedPack の確認に進める。すぐ作る場合は `Tools > Siliq Water > かんたん作成 > 最高品質 Hero 水面を作成` を使う。分割済みメッシュ、Hero 専用マテリアル、Animator、最低限のライト/カメラが一括で作られる。既存の水面がピンク、動かない、高さが出ない場合は `選択中の水面を診断して自動修復` を実行する。

## プリセット

| プリセット | 用途 | 見た目 |
| --- | --- | --- |
| 美しい海 (Clear Sea) | 綺麗な海、リゾート水面、広い透明水 | 青緑の透明感、強めのフレネル反射、細いきらめき |
| 透明プール (Clear Pool) | プール、浅い水、建築ビジュアル | 高い透明度、弱い凹凸、控えめな光網 |
| 室内ブループール (Indoor Blue Pool) | 明るい室内プール、窓反射、アニメ調の青い水面 | 青い透過、広い白ハイライト、強めの反射、控えめな高さ |
| フラッグシップ透明水 (Flagship Crystal) | 製品デモ、メインビジュアル、最上位の透明水 | 厚めの透明感、強い鏡面反射、多層細波、実高さ |
| クリスタルラグーン (Crystal Lagoon) | 透き通った美しさを最優先した浅い水、プール、リゾート水面 | 淡い青緑、強い透過光、控えめな凹凸、強めの水底光 |
| クリスタルラグーン Hero ReadyMaterial | 1000点の見栄え確認、商品ページ、透明水の最初の基準 | さらに高い透明な抜け感、強い反射帯、明るい水底光、低い凹凸 |
| 血の海 (Blood Sea) | ホラー、異世界、赤い液体 | 深い赤、重い粘度感、弱い反射 |
| 液体金属 (Liquid Metal) | 水銀、金属液、SF 表現 | 高反射、高スムースネス、不透明で鏡面寄り |

## 自動処理

プリセット適用時に以下をまとめて行う。

- 適したノーマルマップを割り当てる
- フラッグシップ透明水では専用 2048px normal map と height map を割り当てる
- クリスタルラグーンでは専用 2048px normal map、height map、`Water_Caustics_CrystalLagoon_01.png` を割り当てる
- `M_Siliq_CrystalLagoon_Hero_Ready` は `Water_Normal_CrystalLagoon_Hero_01.png`、`Water_Height_CrystalLagoon_Hero_01.png`、`Water_Caustics_CrystalLagoon_Hero_01.png` を使い、通常 Crystal Lagoon texture の流用に戻さない。`Clarity`、`Reflection Pattern`、`Transmission`、`Bottom Light Strength`、`Caustics Focus`、`Caustics Prism`、`Caustics Scatter` を強めた最高品質確認用として同梱する
- 完成形をすぐ確認できるよう、`PF_Siliq_CrystalLagoon_Complete.prefab` には水面、`Siliq/Pale Pool Floor Mobile` の薄いタイル床、`M_Siliq_CrystalLagoon_CausticsOverlay` の床用光を同梱する。`PF_Siliq_CrystalLagoon_Hero_Complete.prefab` では Hero water material と `M_Siliq_CrystalLagoon_Hero_CausticsOverlay` を使う
- 床に直接光を出したい場合は、床の少し上に薄い Plane を置いて `M_Siliq_CrystalLagoon_CausticsOverlay` を貼る
- 透明な海・プール・フラッグシップ水では `Water_Caustics_Crystal_01.png` を水底の光として割り当てる
- 色、透明度、反射、透過光、きらめきを設定する
- `Clarity`、`Reflection Pattern`、`Bottom Light Strength` を用途ごとに設定し、Crystal Lagoon では水色の濁りを抑え、空や窓の帯状反射と水底光が水越しに見える状態から始める
- `WaterSurfaceAnimator` を追加または更新し、動きを付ける
- Built-in / iOS 系では `Siliq/Water Mobile (Quest)` を使う
- URP プロジェクトでは、`URP Shader` Sample が Import 済みなら `Siliq/Water URP` を優先して使う
- URP Sample が未導入、または Built-in / VRChat / iOS 系では `Siliq/Water Mobile (Quest)` を使う

## 調整の目安

- 綺麗な海: `Normal Strength` を 0.8-1.1、`Opacity` を 0.5-0.7
- プール: `Normal Strength` を 0.25-0.5、`Opacity` を 0.38-0.55
- 室内ブループール: `Normal Strength` を 0.35-0.6、`Opacity` を 0.35-0.5、`Reflection` は高め
- フラッグシップ透明水: `Normal Strength` を 0.55-0.65、`Opacity` を 0.48-0.62、`Reflection` は高め、`Height` は 0.02 以下から始める
- クリスタルラグーン: `Normal Strength` は 0.4 前後、`Clarity` は 0.9 以上、`Reflection Pattern` は 0.42 以上、`Transmission` と `Caustics` は高め、`Caustics Scatter` は 0.58 前後、`Bottom Light Strength` は 1.65 以上、`Height` は 0.01 以下に抑える
- クリスタルラグーン Hero: `Normal Strength` は 0.36 前後、`Opacity` は 0.34 前後、`Clarity` は 0.98、`Reflection Pattern` は 0.58、`Transmission` は 0.98、`Caustics` は 0.84、`Caustics Focus` は 2.4、`Caustics Prism` は 0.24、`Caustics Scatter` は 0.74、`Bottom Light Strength` は 1.9 を基準にする
- 水底の光: プールや浅い海では `Caustics Strength` を 0.3-0.65、暗い場所や深い水では 0.15 以下から始める
- 床用 caustics overlay: `Intensity` は 0.4-0.7、`Tiling` は 1-2 から始める。床が発光しすぎる場合は `Floor Fade` を下げる。
- 氷割れや多角形模様に見える場合: フラッグシップ透明水の `Height Map Influence` は 0 付近、`Height` は 0.02 以下から始める
- 血の海: `Opacity` を 0.6 以上、`Reflection` は控えめ
- 液体金属: `Opacity` は 1、`Refl Strength` と `Smoothness` は高め

## 注意

- 透明感は normal map だけでは作れない。必ず用途別プリセットか透明マテリアルを使う。
- 高品質に見せるには normal map、色、透明度、反射、ハイライト、実高さ、水底の光、ライト、分割メッシュの全部が必要。
- フラッグシップ透明水は Calm の流用ではなく、`Water_Normal_FlagshipCrystal_01.png` と `Water_Height_FlagshipCrystal_01.png` を前提にする。
- クリスタルラグーンは Flagship の流用ではなく、`Water_Normal_CrystalLagoon_01.png`、`Water_Height_CrystalLagoon_01.png`、`Water_Caustics_CrystalLagoon_01.png` を前提にする。Hero では `Water_Normal_CrystalLagoon_Hero_01.png`、`Water_Height_CrystalLagoon_Hero_01.png`、`Water_Caustics_CrystalLagoon_Hero_01.png` を使う。
- `M_Siliq_CrystalLagoon_CausticsOverlay` と `M_Siliq_CrystalLagoon_Hero_CausticsOverlay` は水面 material ではなく床用の加算 overlay。水面には `M_Siliq_CrystalLagoon_Ready`、最高品質確認には `M_Siliq_CrystalLagoon_Hero_Ready` を使う。
- 血や金属液体は「水」ではなく特殊液体なので、色だけでなく反射と凹凸の強さを変える。
- 液体金属は透過させず、不透明で反射を強くした方が破綻しにくい。
