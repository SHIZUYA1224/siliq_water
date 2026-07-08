# 用途別マテリアルプリセットガイド

Siliq Water は、ノーマルマップ単体ではなく「用途別の見た目プリセット」として使うことを前提にする。まず完成形を確認する場合は `PrebakedPack/Preview/preview_crystal_lagoon_complete.png` で方向性を見てから、`PrebakedPack/Prefabs/PF_Siliq_CrystalLagoon_Complete.prefab` を Hierarchy にドラッグする。水面、薄いタイル感のある明るいプール床、床用 caustics overlay、確認用ライトが一体で入っている。

アタッチだけで使う場合は `PrebakedPack/ReadyMaterials/` の `M_Siliq_*_Ready` を Renderer にドラッグする。これらは normal、色、透明度、反射、scroll、水底の光が設定済みで、`WaterSurfaceAnimator` なしでも shader 側で波が動く。

水面オブジェクトを選択して作成・再適用したい場合は、Hierarchy 右クリックから以下を適用する。

`Siliq Water > 用途別マテリアルを適用`

水面マップスタジオでは `目的から始める` のボタンから同じ用途を選べる。ここでは生成レシピ、解像度、スーパーサンプリング、出力マップ、透明マテリアル作成までまとめて推奨値に変わる。

Unity / VRChat 初心者は、まず `Tools > Siliq Water > はじめてガイド` を開く。そこから完成 Prefab の選択、クリスタルラグーン水面作成、既存水面の診断修復、README / 用途別ガイド / PrebakedPack の確認に進める。すぐ作る場合は `Tools > Siliq Water > かんたん作成 > クリスタルラグーン水面を作成` を使う。分割済みメッシュ、マテリアル、Animator、最低限のライト/カメラが一括で作られる。既存の水面がピンク、動かない、高さが出ない場合は `選択中の水面を診断して自動修復` を実行する。

## プリセット

| プリセット | 用途 | 見た目 |
| --- | --- | --- |
| 美しい海 (Clear Sea) | 綺麗な海、リゾート水面、広い透明水 | 青緑の透明感、強めのフレネル反射、細いきらめき |
| 透明プール (Clear Pool) | プール、浅い水、建築ビジュアル | 高い透明度、弱い凹凸、控えめな光網 |
| 室内ブループール (Indoor Blue Pool) | 明るい室内プール、窓反射、アニメ調の青い水面 | 青い透過、広い白ハイライト、強めの反射、控えめな高さ |
| フラッグシップ透明水 (Flagship Crystal) | 製品デモ、メインビジュアル、最上位の透明水 | 厚めの透明感、強い鏡面反射、多層細波、実高さ |
| クリスタルラグーン (Crystal Lagoon) | 透き通った美しさを最優先した浅い水、プール、リゾート水面 | 淡い青緑、強い透過光、控えめな凹凸、強めの水底光 |
| 血の海 (Blood Sea) | ホラー、異世界、赤い液体 | 深い赤、重い粘度感、弱い反射 |
| 液体金属 (Liquid Metal) | 水銀、金属液、SF 表現 | 高反射、高スムースネス、不透明で鏡面寄り |

## 自動処理

プリセット適用時に以下をまとめて行う。

- 適したノーマルマップを割り当てる
- フラッグシップ透明水では専用 2048px normal map と height map を割り当てる
- クリスタルラグーンでは専用 2048px normal map、height map、`Water_Caustics_CrystalLagoon_01.png` を割り当てる
- 完成形をすぐ確認できるよう、`PF_Siliq_CrystalLagoon_Complete.prefab` には水面、`Siliq/Pale Pool Floor Mobile` の薄いタイル床、`M_Siliq_CrystalLagoon_CausticsOverlay` の床用光を同梱する
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
- クリスタルラグーン: `Normal Strength` は 0.4 前後、`Clarity` は 0.9 以上、`Reflection Pattern` は 0.42 以上、`Transmission` と `Caustics` は高め、`Bottom Light Strength` は 1.65 以上、`Height` は 0.01 以下に抑える
- 水底の光: プールや浅い海では `Caustics Strength` を 0.3-0.65、暗い場所や深い水では 0.15 以下から始める
- 床用 caustics overlay: `Intensity` は 0.4-0.7、`Tiling` は 1-2 から始める。床が発光しすぎる場合は `Floor Fade` を下げる。
- 氷割れや多角形模様に見える場合: フラッグシップ透明水の `Height Map Influence` は 0 付近、`Height` は 0.02 以下から始める
- 血の海: `Opacity` を 0.6 以上、`Reflection` は控えめ
- 液体金属: `Opacity` は 1、`Refl Strength` と `Smoothness` は高め

## 注意

- 透明感は normal map だけでは作れない。必ず用途別プリセットか透明マテリアルを使う。
- 高品質に見せるには normal map、色、透明度、反射、ハイライト、実高さ、水底の光、ライト、分割メッシュの全部が必要。
- フラッグシップ透明水は Calm の流用ではなく、`Water_Normal_FlagshipCrystal_01.png` と `Water_Height_FlagshipCrystal_01.png` を前提にする。
- クリスタルラグーンは Flagship の流用ではなく、`Water_Normal_CrystalLagoon_01.png`、`Water_Height_CrystalLagoon_01.png`、`Water_Caustics_CrystalLagoon_01.png` を前提にする。
- `M_Siliq_CrystalLagoon_CausticsOverlay` は水面 material ではなく床用の加算 overlay。水面には `M_Siliq_CrystalLagoon_Ready` を使う。
- 血や金属液体は「水」ではなく特殊液体なので、色だけでなく反射と凹凸の強さを変える。
- 液体金属は透過させず、不透明で反射を強くした方が破綻しにくい。
