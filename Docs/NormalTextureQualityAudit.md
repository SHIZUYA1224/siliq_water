# Normal Texture Quality Audit

最終監査日: 2026-07-11

## 対象と結論

`PrebakedPack/Textures/Water_Normal_*_01.png` の9枚を、生PNG、Unity import、1枚表示、2x2 Repeat、簡易ライティング表示で監査しました。全9枚を2048pxで再構成し、既存GUIDとMaterial参照は維持しています。

画像生成で得た自然な波形は形状設計のソースとしてのみ使い、そのままnormal mapにはしていません。周期境界へ補正し、用途別の低・中・微細波を合成した後、各ピクセルを正のZを持つ単位ベクトルへ変換しています。

| Texture | 用途 | 修正内容 |
|---|---|---|
| `Water_Normal_Calm_01.png` | 湖、静かな海 | 均一なブラシ状ノイズを、方向の異なる3帯域の穏やかな波へ変更 |
| `Water_Normal_CrystalLagoon_01.png` | 透明な浅い水 | 平坦な一方向波を、静かな領域を含む交差波へ変更 |
| `Water_Normal_CrystalLagoon_Hero_01.png` | 最高品質確認 | 弱すぎた法線を強化し、広い波と細かな反射崩れを分離 |
| `Water_Normal_Cyber_01.png` | 液体金属、粘性液体 | 布目・交差格子を削除し、滑らかな広い折れへ変更 |
| `Water_Normal_FlagshipCrystal_01.png` | 海、製品デモ | 縦櫛状の細粒を削除し、不規則なうねりと風波へ変更 |
| `Water_Normal_Pool_01.png` | プール、室内水面 | Voronoi・氷割れ・多角形面を削除し、浅い連続交差波へ変更 |
| `Water_Normal_Ripple_01.png` | 雨天の基礎水面 | 強い静止スタンプを弱めた。リングの拡大は `WaterRippleEmitter` が担当 |
| `Water_Normal_Stream_01.png` | 川、流れ | 直線バーと格子を削除し、流向を保った曲線と渦へ変更 |
| `Water_Normal_WaterTable_01.png` | ガラス水盤 | 完全な機械円をわずかに歪ませ、静かな基礎波と中央リングを分離 |

## 自動品質ゲート

EditMode test `BundledNormalTextures_MeetProductQualityGates` が次を生PNGから検証します。

- 9枚すべて2048x2048
- RGBをデコードした法線長の平均誤差が0.0025以下、最大誤差が0.008以下
- Zが正で、XY平均偏りが0.002以下
- 用途別の法線強度範囲と方向異方性
- 64pxブロックで低周波と微細波が両方存在すること
- 局所エネルギーに静かな領域と動く領域の差があること
- Repeat境界の差が内部の隣接差の1.5倍以下
- Poolに硬いセル境界、Cyberに布目、Streamに1pxバーがないこと

`BundledNormalTextures_UseHighQualityPlatformImports` はNormalMap、Linear、Repeat、Trilinear、mipmap、anisotropic 2、PC 2048px高品質圧縮、Quest/iOS 1024px ASTC 5x5を固定します。

## 運用ルール

- normal PNGへ色補正、sRGB変換、輪郭強調を適用しない。
- 強い高さはheight mapと分割メッシュで扱い、normalだけで厚みを作らない。
- 水底causticsは水面normalへ焼き込まず、床・水底の別メッシュへ貼る。
- 波紋をUVスクロールで表現しない。接触位置と時刻から半径を増やす。
- normalを差し替えた場合は9枚一括のEditMode testと2x2 Repeat表示を実行する。
