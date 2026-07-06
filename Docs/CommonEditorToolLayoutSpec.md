# 共通 Editor ツール UI 仕様書

この仕様は、Siliq 系 Unity EditorWindow の共通デザインとして使う。対象は、水面マップスタジオのような「左で設定し、右で結果を見ながら調整する」生成・編集ツール全般。

## 目的

- 操作中にプレビューが画面外へ流れないようにする
- 初見でも「どこから触るか」が分かる順番にする
- 他ツールでも同じ操作感にして、学習コストを下げる
- 書き出しや適用などの最終アクションを迷わず実行できるようにする

## 基本レイアウト

広いウィンドウでは 2 ペイン構成にする。

- 左: 操作パネル
- 右: 固定プレビュー

狭いウィンドウでは 1 カラムに戻し、上から操作、下にプレビューを並べる。

推奨値:

| 項目 | 値 |
| --- | --- |
| 最小ウィンドウサイズ | 720 x 560 |
| 2 ペイン切替幅 | 820 px 以上 |
| 左ペイン幅 | ウィンドウ幅の 38% |
| 左ペイン最小幅 | 360 px |
| 左ペイン最大幅 | 520 px |
| ペイン間余白 | 10 px |
| 右プレビュー最小幅 | 280 px |

## 左ペイン

左ペインは縦スクロール可能にする。右プレビューはスクロールに巻き込まない。

標準の並び順:

1. プリセット / 入力元
2. 全体設定
3. レイヤー / 要素編集
4. 書き出し / 適用

各セクションは番号付き見出しにする。複雑なツールでも、ユーザーが上から順に触れば成立する構造を守る。

### 1. プリセット / 入力元

用途:

- 最初の状態を選ぶ
- プロファイルやアセットを読み込む
- JSON などで設定を共有する

ルール:

- プリセット選択と「適用」ボタンは同じ行に置く
- 既存設定を壊す操作は、実行後にプレビューを即更新する
- プロファイル読み込み後は foldout 状態をリセットして、内容を見失わないようにする

### 2. 全体設定

用途:

- 解像度、品質、強さ、シードなど、結果全体に効く設定をまとめる

ルール:

- 生成結果に大きく影響する設定を上へ置く
- ランダムシードは数値入力とランダム化ボタンを同じ行に置く
- 高コスト設定には tooltip で負荷を明記する

### 3. レイヤー / 要素編集

用途:

- 波レイヤー、エフェクト要素、マスクなど、積み重ねる構造を編集する

ルール:

- 各レイヤーは foldout で折りたためる
- 有効/無効、名前、種類、上下移動、複製、削除をヘッダーにまとめる
- 削除・複製・移動の直後はプレビューを dirty にする
- パラメータ名は技術用語だけにせず、見た目の意味が分かる日本語を優先する

### 4. 書き出し / 適用

用途:

- 生成結果をファイル、マテリアル、シーン、Prefab などへ反映する

ルール:

- 出力対象の選択を先に置く
- 実行ボタンはセクション内で最も目立つ高さにする
- 実行できない状態ではボタンを disabled にする
- 長い処理は progress bar を出し、finally で必ず消す

## 右ペイン: 固定プレビュー

右ペインは常に表示する。左ペインをスクロールしても動かさない。

構成:

1. 見出し
2. 表示対象の切り替え
3. 表示モード切り替え
4. 時間 / 位相スライダー
5. プレビュー本体

推奨ルール:

- 見出しは「固定プレビュー」とする
- プレビュー本体は正方形を基本にする
- プレビューサイズは右ペイン幅とウィンドウ高さの小さい方に合わせる
- 表示対象ボタンは右ペイン幅に応じて 2-3 列へ変える
- アニメ再生中は応答性を優先し、重い品質設定を一時的に下げてもよい
- 生成結果がない場合は暗いプレースホルダーを表示する

## レスポンシブ挙動

2 ペイン条件:

```csharp
bool useWideLayout = position.width >= 820f;
```

広い場合:

```csharp
float leftWidth = Mathf.Clamp(position.width * 0.38f, 360f, 520f);
float previewPaneWidth = Mathf.Max(280f, position.width - leftWidth - 10f - 18f);
```

狭い場合:

- 操作とプレビューを 1 カラムにする
- 既存ツールとの互換のため、縦スクロールを許可する
- プレビューは下に出てもよいが、幅に合わせて最大 320 px 程度に抑える

## 状態管理

必須状態:

- 設定本体
- 左ペインの scroll
- プレビュー対象 index
- プレビュー dirty flag
- アニメ再生 flag
- プレビュー時間

推奨:

- 設定本体は EditorPrefs または ScriptableObject profile で保存する
- プレビュー texture は `HideFlags.HideAndDontSave` にする
- Window close 時に preview texture を `DestroyImmediate` する
- パラメータ変更時は `EditorGUI.BeginChangeCheck()` / `EndChangeCheck()` で preview dirty と保存をまとめる

## 実装テンプレート

```csharp
void OnGUI()
{
    if (position.width >= WideLayoutThreshold)
    {
        DrawWideLayout();
    }
    else
    {
        DrawCompactLayout();
    }

    if (previewDirty && Event.current.type == EventType.Repaint)
    {
        RegeneratePreview();
    }
}

void DrawWideLayout()
{
    float leftWidth = Mathf.Clamp(position.width * 0.38f, 360f, 520f);
    float previewWidth = Mathf.Max(280f, position.width - leftWidth - 28f);

    using (new EditorGUILayout.HorizontalScope())
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(leftWidth)))
        {
            DrawControlScroll();
        }

        GUILayout.Space(10f);

        using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
        {
            DrawPreviewSection(previewWidth, fixedPane: true);
        }
    }
}
```

## 禁止事項

- プレビューを左ペインのスクロール内に置かない
- 操作ボタンをプレビュー下へ追いやらない
- セクション順をツール都合でばらばらにしない
- 生成結果の確認に必要な情報を折りたたみ内だけに隠さない
- 実行ボタンを無効条件なしで常に押せる状態にしない

## 移植チェックリスト

- [ ] ウィンドウ幅 820 px 以上で左操作 / 右固定プレビューになる
- [ ] 左ペインだけがスクロールする
- [ ] 右プレビューはパラメータ編集中も見え続ける
- [ ] 狭い幅では 1 カラムへ破綻なく戻る
- [ ] セクション見出しが `1. プリセット` / `2. 全体設定` / `3. レイヤー` / `4. 書き出し` 相当になっている
- [ ] プレビュー対象、タイル表示、アニメ再生、時間スライダーがプレビュー側にある
- [ ] 変更時に preview dirty が立つ
- [ ] 重いプレビュー生成は Repaint タイミングで行う
- [ ] 実行不能な書き出し/適用ボタンは disabled になる
- [ ] Unity batch compile でエラーがない

## 受け入れ基準

新しい Editor ツールへ適用する場合、以下を満たせばこの共通デザインに準拠していると判断する。

- 初回起動時、左上から操作すれば結果を作れる
- プレビューを探すためにスクロールする必要がない
- パラメータ変更からプレビュー反映までの関係が明確
- 書き出し/適用の前に、何が出力されるか右側で確認できる
- 水面マップスタジオと同じ操作感で使える
