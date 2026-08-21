# ComfyUI キャラクターグラフィック生成・連携設計

## 1. 目的

GuildFrontierSim のキャラクターメイクや NPC 追加に使うグラフィックを、Unity とは独立した WPF ツールから ComfyUI で生成できるようにする。

画像生成、候補比較、生成履歴、ファイル出力は WPF ツールの責務とする。Unity は ComfyUI と通信せず、WPF ツールが書き出した採用済み画像とメタデータだけを取り込む。ComfyUI や WPF ツールが未導入、停止中、または生成に失敗した場合でも、Unity プロジェクトとゲームは既定画像を使って継続できなければならない。

本ドキュメントは連携方針を定めるものであり、現在の最優先事項である Phase 1（経営自動シミュレーション）の実装範囲を増やすものではない。生成 UI や本格的なキャラクターメイクへの統合は、原則として Phase 5 で行う。

---

## 2. 対象とする画像

初期対象はキャラクターの立ち絵またはバストアップ画像とする。

- プレイヤーキャラクター
- 登録によって作成する冒険者
- スカウト対象 NPC
- 必要に応じて敵対人物やイベント人物

表情差分、ポーズ差分、戦闘用スプライト、Live2D 用素材などは初期対象外とする。ただし、将来 `ImageVariant` を追加して複数画像を関連付けられる構造にしておく。

---

## 3. 基本方針

- WPF ツールが ComfyUI の HTTP API を利用し、Unity とゲーム本体には ComfyUI 通信機能を組み込まない。
- ComfyUI はローカル PC またはユーザーが明示的に設定したサーバーで動作させる。
- WPF の UI スレッドを生成完了まで停止させず、キャンセル可能な非同期処理にする。
- プロンプト文字列だけでなく、使用ワークフロー、モデル、seed、画像サイズなども生成記録に残す。
- WPF と Unity の受け渡しは、画像ファイル、生成メタデータ JSON、安定した `visualId` から成る明示的な出力形式を使う。
- 生成画像そのものを ScriptableObject に埋め込まず、Unity 側では画像の論理 ID とアセット参照を関連付ける。
- 初期実装は開発者向け WPF ツールに限定し、ビルド後のプレイヤーによる Runtime 生成は対象外とする。
- API アドレス、認証情報、ローカルのモデルパスをリポジトリへコミットしない。
- 画像生成は任意機能とし、既定画像へのフォールバックを必ず用意する。

---

## 4. WPF ツールによる運用

### 4.1 開発者向け生成ツール

開発者が独立した WPF アプリケーションでキャラクター画像を生成し、候補を確認してから Unity インポート用フォルダへ書き出す。Unity Editor を起動せずに、生成、再生成、比較、採用、生成履歴の確認を完結できるようにする。

利点：

- Unity のコンパイルや Play Mode と画像生成作業を切り離せる
- WPF のデータバインディングを利用して候補一覧や編集 UI を構築しやすい
- 配布ビルドや Unity プロジェクトに ComfyUI 接続機能を含めなくてよい
- 採用画像を人間が確認できる
- Unity は決められた形式の成果物をインポートするだけでよい
- ビルドの再現性を保ちやすい

WPF ツールは作業データを Unity の `Assets` へ直接書き込まない。採用操作で Unity インポート用の出力フォルダへ画像と JSON を書き出し、その後に手動コピーまたは専用インポーターで `Assets/Game/Art/Characters/Generated/` へ取り込む。

### 4.2 Unity 側の役割

Unity 側は次の処理だけを担当する。

- WPF ツールの出力形式を検証してインポートする
- `visualId` と Sprite または Texture アセットを関連付ける
- キャラクターデータから画像を表示する
- 欠損時に既定画像へフォールバックする

Unity から WPF ツールや ComfyUI を起動する機能は初期対象外とする。Runtime 生成が将来必要になった場合も、本設計へ混在させず別機能として再検討する。

---

## 5. システム構成

```text
WPF Character Art Tool
            |
            v
CharacterImageGenerationService
   |        |              |
   |        |              +--> GenerationRecord 保存
   |        +--> PromptBuilder
   v
IImageGenerationClient
   |
   +--> ComfyUIClient ---- HTTP ----> ComfyUI
   |
   +--> MockImageGenerationClient（テスト用）
            |
            v
 Generated Candidate -> 検証・採用 -> Export Package
                                           |
                                           v
                                  Unity Importer / Manual Import
                                           |
                                           v
                                  CharacterVisualReference
```

`CharacterImageGenerationService` 以下は WPF ツール内に置く。Unity ゲームロジックはこのツールのアセンブリを参照せず、共通契約であるエクスポート JSON の形式だけを共有する。キャラクターの戦闘・忠誠度・状態管理は画像生成状態に依存させない。

---

## 6. 推奨クラスと責務

### `ComfyUISettings`（WPF）

接続設定を保持する。

- Base URL（初期値例：`http://127.0.0.1:8188`）
- 接続・生成タイムアウト
- ポーリング間隔
- 使用するワークフローテンプレート ID
- 作業フォルダ
- Unity インポート用出力フォルダ

WPF のローカル設定ファイルとしてユーザープロファイル配下へ保存する。認証トークンや各 PC 固有の絶対パスをリポジトリ内の設定へ書き込まない。

### `CharacterAppearanceSpec`（WPF）

ゲーム側の外見指定を、ComfyUI 固有のノード構造から独立させて表現する値オブジェクト。

例：

- body type / age group
- hair style / hair color
- eye color
- outfit / job motif
- personality or expression
- equipment
- background style
- negative tags

表示名と生成用タグを混同しない。選択肢は後からローカライズできる ID ベースで保持する。

### `PromptBuilder`（WPF）

`CharacterAppearanceSpec` と画風プリセットから positive / negative prompt を組み立てる。テンプレートや固定品質タグは設定データ側へ置き、コードへの大量ハードコードを避ける。

### `ComfyUIWorkflowTemplate`（WPF）

ComfyUI の API format の workflow JSON と、差し替える入力箇所の対応を管理する。

差し替え対象の例：

- positive prompt
- negative prompt
- seed
- width / height
- batch size
- checkpoint name
- output filename prefix

ノード番号を C# コード中へ散在させず、テンプレートごとのバインディング定義に集約する。

### `IImageGenerationClient`（WPF）

画像生成基盤を抽象化する。

想定操作：

- 接続確認
- 生成要求の送信
- 進捗または状態確認
- キャンセル
- 出力画像の取得

### `ComfyUIClient`（WPF）

ComfyUI API との通信を担当する。JSON の組み立て、`prompt_id` の管理、履歴確認、画像取得を行う。`HttpClient` とキャンセルトークンを使った非同期処理にし、ViewModel へ HTTP の詳細を漏らさない。

### `CharacterImageGenerationService`（WPF）

生成処理全体の窓口。

- 入力検証
- prompt と workflow の構築
- ジョブ状態の管理
- 結果画像の検証
- 保存処理への引き渡し
- エラーをユーザー向け状態へ変換

### `CharacterArtExportService`（WPF）

採用された候補を Unity 向けパッケージとして書き出す。

- `visualId` を新規発行または維持する
- 出力画像を規定の名前、形式、寸法へそろえる
- `character-visual.json` を生成する
- 画像とメタデータのハッシュを記録する
- 一時フォルダへ完成物を作ってから出力先へ確定する
- 同名データを上書きする前に確認する

### `GenerationRecord`（WPF）

再現と監査のための生成記録。

- generation ID
- character ID（割り当て済みの場合）
- 作成日時
- workflow template ID / version
- seed
- prompt / negative prompt
- model / sampler / scheduler
- width / height
- ComfyUI prompt ID
- 出力ファイル名とハッシュ
- 採用・却下状態

### `CharacterVisualReference`

キャラクターのランタイムデータが保持する画像参照。

- visual ID
- source type（BuiltIn / Generated / Custom / Fallback）
- portrait key または相対ファイル名
- variant ID（将来用）

絶対パスや `Texture2D` 本体をセーブデータへ直接保存しない。

### `CharacterArtImporter`（Unity Editor、将来実装）

WPF ツールが出力したパッケージを Unity へ取り込む薄い Editor 機能。

- JSON schema version と必須項目を検証する
- 許可された画像形式、寸法、容量を検証する
- 規定の `Assets/Game/Art/Characters/Generated/<visual-id>/` へコピーする
- TextureImporter の設定を統一する
- visual ID と Unity アセットの対応データを更新する
- 同一 ID の衝突や更新をユーザーへ表示する

初期段階では手動コピーでもよいが、出力形式はこのインポーターを追加できる形に固定する。

---

## 7. ComfyUI API の処理フロー

1. WPF ツールから接続確認を行う。
2. workflow テンプレートを複製する。
3. prompt、negative prompt、seed、サイズなどを workflow に反映する。
4. ComfyUI の prompt queue に生成要求を送信する。
5. 返却された `prompt_id` をジョブへ保存する。
6. WebSocket または履歴 API のポーリングで完了を待つ。
7. 履歴に記録された出力情報から画像を取得する。
8. PNG/JPEG、最大容量、寸法を検証する。
9. 一時領域へ保存してプレビューする。
10. ユーザーが採用した画像だけを WPF のライブラリへ正式保存し、`GenerationRecord` を更新する。
11. エクスポート操作で画像と `character-visual.json` を Unity インポート用フォルダへ書き出す。
12. Unity 側でパッケージを検証し、`CharacterVisualReference` と画像アセットの対応を登録する。

API のエンドポイントやレスポンス形式は利用する ComfyUI のバージョンで確認し、C# 側では通信 DTO とゲーム用モデルを分離する。

---

## 8. ワークフロー管理

ComfyUI で動作確認済みの workflow を API format で書き出し、プロジェクト用テンプレートとして管理する。

推奨配置例：

```text
Tools/
  GuildFrontier.CharacterArtTool/
    GuildFrontier.CharacterArtTool.sln
    src/
      GuildFrontier.CharacterArtTool.App/          # WPF / View / ViewModel
      GuildFrontier.CharacterArtTool.Core/         # 生成・エクスポートのユースケース
      GuildFrontier.CharacterArtTool.Infrastructure/ # ComfyUI・ファイル実装
    tests/
    Workflows/
      CharacterPortrait_v1.json
    Presets/
Assets/Game/Art/Characters/Generated/
Docs/
  ComfyUICharacterGraphicsIntegration.md
```

WPF ツールは .NET の独立ソリューションとし、Unity の `.csproj` や UnityEngine を参照しない。最初は過剰な多層化を避けてもよいが、UI、生成ロジック、ComfyUI 通信は分離する。UI は MVVM を基本とし、コードビハインドへ生成処理を集中させない。

workflow を変更すると同じ prompt と seed でも結果が変わる可能性があるため、テンプレート ID と version を必ず付ける。checkpoint、LoRA、custom node は外部依存一覧として別途記録する。大容量モデルファイルは Git に含めない。

custom node に強く依存した workflow は環境構築が壊れやすいため、最初のテンプレートは標準的な text-to-image 構成を優先する。

---

## 9. ファイル保存方針

### WPF ツールの作業ライブラリ

作業ライブラリは Unity プロジェクト外、または Git 管理外の専用フォルダへ置く。

```text
CharacterArtWorkspace/
  Candidates/<generation-id>/
  Library/<visual-id>/portrait.png
  Library/<visual-id>/generation.json
  Export/<visual-id>/portrait.png
  Export/<visual-id>/character-visual.json
```

候補画像と採用済みライブラリを分け、候補を Unity プロジェクトへ直接流し込まない。

### Unity に取り込んだ画像

```text
Assets/Game/Art/Characters/Generated/<visual-id>/portrait.png
Assets/Game/Art/Characters/Generated/<visual-id>/character-visual.json
```

採用画像を Git に含めるかは、容量とチーム運用を見て決める。含める場合はライセンス確認済みの画像だけにする。

### 一時ファイル

候補画像は一時フォルダへ置き、採用または明示的な保存を行うまで正式データにしない。古い一時ファイルを削除する処理は、対象ディレクトリを厳密に限定する。

---

## 10. UI 方針

画像生成 UI は WPF で作成し、Unity の Canvas、Prefab、EditorWindow は使用しない。WPF 側は MVVM を基本とし、画面状態と生成ジョブ状態を ViewModel で管理する。Unity のキャラクター表示 UI は既存の開発指示どおり Editor 上で構築し、画像参照を Inspector またはデータから設定する。

最低限の操作：

- ComfyUI 接続状態表示
- 外見項目の選択
- seed の固定 / ランダム切り替え
- 生成開始 / キャンセル
- 処理中表示
- 候補画像のプレビュー
- 再生成
- 採用 / 破棄
- エラーメッセージと既定画像選択
- Unity インポート用フォルダへのエクスポート
- エクスポート済みか、未反映の変更があるかの表示

同じボタンを連打して重複ジョブを大量投入できないようにする。

---

## 11. エラー処理とフォールバック

次の状況を区別して扱う。

- ComfyUI に接続できない
- workflow が不正
- 必要な checkpoint / custom node がない
- queue が混雑している
- タイムアウトまたはキャンセル
- ComfyUI 側の生成エラー
- 画像取得失敗
- 保存容量不足
- 不正または大きすぎる画像

いずれの場合もキャラクター作成データを失わず、再試行、設定確認、既定画像の使用を選択できるようにする。HTTP の技術的な詳細をそのまま UI に出さず、詳細ログと利用者向けメッセージを分ける。

---

## 12. セキュリティ・安全・権利

- 初期設定では localhost のみを想定し、外部アドレスへの接続はユーザーが明示的に有効化する。
- 外部 ComfyUI を利用する場合、通信内容や prompt、生成画像が第三者サーバーへ送られることを表示する。
- URL、出力ファイル名、API レスポンスを信頼せず、パストラバーサルを防止する。
- ダウンロード画像の MIME type、拡張子、容量、寸法を検証する。
- API キーなどをリポジトリ内ファイル、ログ、Unity のセーブデータへ平文保存しない。
- checkpoint、LoRA、学習素材、生成物のライセンスと商用利用条件を確認する。
- 実在人物、第三者 IP、不適切表現などに関するプロジェクトの生成ルールを定める。
- 配布時は、使用モデルと必要なクレジットを追跡できるよう `GenerationRecord` に情報を残す。

---

## 13. テスト方針

ComfyUI を起動しなくても自動テストできるよう `MockImageGenerationClient` を用意する。

主なテスト対象：

- AppearanceSpec から期待する prompt が構築される
- workflow の指定箇所だけが置換される
- 同一設定と seed の生成記録が同一内容になる
- 接続失敗、タイムアウト、キャンセルを正しく状態変換できる
- 不正なファイル名や過大画像を拒否する
- 生成画像がなくても既定画像へフォールバックする
- セーブ・ロード後に visual ID から画像を復元できる

WPF 側は xUnit などで Core と Infrastructure の単体テストを行う。ComfyUI 実機との結合テストは手動または明示的に有効化したテストとして分け、通常テストを外部サービスへ依存させない。Unity 側ではインポート JSON の検証、visual ID の解決、既定画像へのフォールバックを EditMode テストする。

---

## 14. 段階的な導入計画

### Phase 1 と同時に行う最小準備

- キャラクター画像が未設定でも動作するよう既定画像を用意する
- 将来必要になった時点で `CharacterVisualReference` を追加できるよう、キャラクター ID を安定させる
- 戦闘・経営ロジックを画像アセットから独立させる

この時点では ComfyUI 通信コードを実装しない。

### ComfyUI 連携 Step 1：技術検証

- 利用モデルとライセンスを決める
- 最小 workflow を ComfyUI で作成する
- API format の workflow を保存する
- 最小の .NET コンソール検証または WPF 試作から接続確認、1 枚生成、取得までを検証する

### Step 2：WPF 生成ツール

- 独立した .NET / WPF ソリューションを `Tools/` 配下へ作成する
- Settings、WorkflowTemplate、PromptBuilder を実装する
- WPF 画面から生成・プレビュー・比較・採用を可能にする
- GenerationRecord と採用画像を作業ライブラリへ保存する
- Unity 向けエクスポートを実装する
- Mock を使った .NET 単体テストを追加する

### Step 3：Unity インポートとキャラクターデータ統合

- 最初は規定フォルダへの手動コピーで受け渡しを検証する
- 必要になった時点で CharacterArtImporter を追加する
- CharacterVisualReference をキャラクター定義またはランタイムデータへ関連付ける
- 一覧、詳細、戦闘など共通の画像表示サービスを作る
- 欠損時のフォールバックを確認する

### Step 4：Phase 5 キャラクターメイク統合

- WPF ツールで作成した visual ID をキャラクターメイクや NPC 定義から選択できるようにする
- Unity 内では生成を行わず、インポート済み画像の選択と表示に限定する
- NPC 作成やスカウト対象にも同じ画像参照方式を再利用する

### Step 5：運用改善（任意）

- WPF から複数キャラクターを一括生成する
- タグ、検索、生成履歴比較を追加する
- Unity インポーターで更新差分を表示する
- 画像のトリミング、背景除去、規格検証を追加する
- アセット更新時に参照切れがないか検査する

---

## 15. 初回実装時の完了条件

最初の ComfyUI 連携実装は、次を満たせば完了とする。

1. ComfyUI と WPF ツールが停止していても Unity Editor とゲームの通常動作に影響しない。
2. WPF ツール上で接続確認ができる。
3. バージョン付き workflow に prompt と seed を渡し、1 枚生成できる。
4. 生成中に WPF の UI がフリーズせず、生成をキャンセルできる。
5. 生成画像をプレビューし、採用または破棄できる。
6. 採用画像と GenerationRecord が対応付けて保存される。
7. 採用画像と `character-visual.json` を Unity インポート用フォルダへ書き出せる。
8. Unity は ComfyUI 通信コードを持たず、visual ID を通じてインポート済み画像を表示できる。
9. 画像欠損時は既定画像を表示できる。
10. Mock を用いた WPF 側の主要な単体テストが通る。
11. 必要なモデル、custom node、ライセンス、セットアップ手順が文書化される。

---

## 16. 未決定事項

実装前に次を決定する。

- 目標画風と画像仕様（バストアップ / 全身、解像度、背景透過など）
- 使用 checkpoint / LoRA とライセンス
- 最初の workflow と必要 custom node
- 生成画像を Git 管理するか
- WPF ツールの対象 .NET バージョン
- WPF 作業ライブラリと Unity インポート用出力フォルダの既定位置
- Unity への取り込みを当面手動にするか、最初から Editor インポーターを作るか
- `character-visual.json` の schema version 1 の詳細
- CharacterDefinition とランタイムキャラクターのどちらへ visual ID を持たせるか
- WPF ツールの配布方法（self-contained / framework-dependent）

これらは Phase 1 のゲームサイクルを完成させた後、ComfyUI 連携 Step 1 の技術検証時に確定する。
