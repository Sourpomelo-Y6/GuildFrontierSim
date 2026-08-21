# ComfyUI キャラクターグラフィック生成・連携設計

## 1. 目的

GuildFrontierSim のキャラクターメイクや NPC 追加時に、ComfyUI を利用してキャラクターグラフィックを生成できるようにする。

この仕組みはゲーム本体から独立した補助機能として設計する。ComfyUI が未導入、停止中、または生成に失敗した場合でも、既定画像を使ってゲームを継続できなければならない。

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

- Unity は ComfyUI の HTTP API を利用し、ComfyUI 本体をゲームへ組み込まない。
- ComfyUI はローカル PC またはユーザーが明示的に設定したサーバーで動作させる。
- Unity のメインスレッドを生成完了まで停止させない。
- プロンプト文字列だけでなく、使用ワークフロー、モデル、seed、画像サイズなども生成記録に残す。
- 生成画像そのものを ScriptableObject に埋め込まず、画像の論理 ID と保存先を参照する。
- 開発用の画像生成と、ビルド後にプレイヤーが行う画像生成を分離して考える。
- API アドレス、認証情報、ローカルのモデルパスをリポジトリへコミットしない。
- 画像生成は任意機能とし、既定画像へのフォールバックを必ず用意する。

---

## 4. 想定する利用モード

### 4.1 Editor 生成モード（先に実装）

開発者が Unity Editor 上でキャラクター画像を生成し、確認・採用した画像をプロジェクトのアセットとして取り込む。

利点：

- 配布ビルドに ComfyUI 接続機能を含めなくてよい
- 採用画像を人間が確認できる
- インポート設定や Addressables 化を事前に行える
- ビルドの再現性を保ちやすい

採用した画像は `Assets/Game/Art/Characters/Generated/` などへ配置し、Unity の通常アセットとして扱う。生成途中の候補や一時ファイルはプロジェクト外、または Git 管理外のフォルダへ保存する。

### 4.2 Runtime 生成モード（将来拡張）

プレイヤーがキャラクターメイク中に生成を要求し、完成した画像をセーブデータ側へ保存する。

このモードは環境依存性、待ち時間、容量、利用規約、安全対策が増えるため Phase 5 以降の追加機能とする。Runtime 生成画像は `Application.persistentDataPath` 配下へ保存し、`Assets` フォルダには書き込まない。

---

## 5. システム構成

```text
Character Make / Editor Tool
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
 Generated Image -> 検証・採用 -> CharacterVisualReference
```

ゲームロジックは `IImageGenerationClient` や HTTP 通信を直接呼び出さず、`CharacterImageGenerationService` を介する。キャラクターの戦闘・忠誠度・状態管理は画像生成状態に依存させない。

---

## 6. 推奨クラスと責務

### `ComfyUISettings`

接続設定を保持する。

- Base URL（初期値例：`http://127.0.0.1:8188`）
- 接続・生成タイムアウト
- ポーリング間隔
- 使用するワークフローテンプレート ID
- Editor 生成を許可するか
- Runtime 生成を許可するか

機密情報を含まない既定値は ScriptableObject にできる。認証トークンが必要な場合は別のローカル設定として保存し、Git 管理対象外にする。

### `CharacterAppearanceSpec`

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

### `PromptBuilder`

`CharacterAppearanceSpec` と画風プリセットから positive / negative prompt を組み立てる。テンプレートや固定品質タグは設定データ側へ置き、コードへの大量ハードコードを避ける。

### `ComfyUIWorkflowTemplate`

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

### `IImageGenerationClient`

画像生成基盤を抽象化する。

想定操作：

- 接続確認
- 生成要求の送信
- 進捗または状態確認
- キャンセル
- 出力画像の取得

### `ComfyUIClient`

ComfyUI API との通信を担当する。JSON の組み立て、`prompt_id` の管理、履歴確認、画像取得を行う。Unity の `UnityWebRequest` または対応する HTTP クライアントを使用し、非同期処理にする。

### `CharacterImageGenerationService`

生成処理全体の窓口。

- 入力検証
- prompt と workflow の構築
- ジョブ状態の管理
- 結果画像の検証
- 保存処理への引き渡し
- エラーをユーザー向け状態へ変換

### `GenerationRecord`

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

---

## 7. ComfyUI API の処理フロー

1. Unity から接続確認を行う。
2. workflow テンプレートを複製する。
3. prompt、negative prompt、seed、サイズなどを workflow に反映する。
4. ComfyUI の prompt queue に生成要求を送信する。
5. 返却された `prompt_id` をジョブへ保存する。
6. WebSocket または履歴 API のポーリングで完了を待つ。
7. 履歴に記録された出力情報から画像を取得する。
8. PNG/JPEG、最大容量、寸法を検証する。
9. 一時領域へ保存してプレビューする。
10. ユーザーが採用した画像だけを正式保存し、`CharacterVisualReference` と `GenerationRecord` を更新する。

API のエンドポイントやレスポンス形式は利用する ComfyUI のバージョンで確認し、C# 側では通信 DTO とゲーム用モデルを分離する。

---

## 8. ワークフロー管理

ComfyUI で動作確認済みの workflow を API format で書き出し、プロジェクト用テンプレートとして管理する。

推奨配置例：

```text
Assets/Game/ComfyUI/
  Settings/
  WorkflowTemplates/
    CharacterPortrait_v1.json
  Presets/
Docs/
  ComfyUICharacterGraphicsIntegration.md
```

workflow を変更すると同じ prompt と seed でも結果が変わる可能性があるため、テンプレート ID と version を必ず付ける。checkpoint、LoRA、custom node は外部依存一覧として別途記録する。大容量モデルファイルは Git に含めない。

custom node に強く依存した workflow は環境構築が壊れやすいため、最初のテンプレートは標準的な text-to-image 構成を優先する。

---

## 9. ファイル保存方針

### Editor で採用した画像

```text
Assets/Game/Art/Characters/Generated/<visual-id>/portrait.png
Assets/Game/Art/Characters/Generated/<visual-id>/generation.json
```

採用画像を Git に含めるかは、容量とチーム運用を見て決める。含める場合はライセンス確認済みの画像だけにする。

### Runtime 生成画像

```text
Application.persistentDataPath/
  GeneratedCharacters/<visual-id>/portrait.png
  GeneratedCharacters/<visual-id>/generation.json
```

セーブデータには `visual-id` と相対パスを保存する。画像が削除・破損していた場合は既定画像を表示する。

### 一時ファイル

候補画像は一時フォルダへ置き、採用または明示的な保存を行うまで正式データにしない。古い一時ファイルを削除する処理は、対象ディレクトリを厳密に限定する。

---

## 10. UI 方針

既存の開発指示どおり、UI の GameObject を大量のコードで自動生成しない。EditorWindow またはキャラクターメイク画面の Hierarchy / Prefab を Editor 上で構築し、スクリプト参照は Inspector から設定する。

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
- API キーなどを ScriptableObject、ログ、セーブデータへ平文保存しない。
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

ComfyUI 実機との結合テストは手動または明示的に有効化したテストとして分け、通常の Unity テストを外部サービスへ依存させない。

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
- Unity から接続確認、1 枚生成、取得までを検証する

### Step 2：Editor ツール

- Settings、WorkflowTemplate、PromptBuilder を実装する
- EditorWindow から生成・プレビュー・採用を可能にする
- GenerationRecord と採用画像を保存する
- Mock を使った EditMode テストを追加する

### Step 3：キャラクターデータ統合

- CharacterVisualReference をキャラクター定義またはランタイムデータへ関連付ける
- 一覧、詳細、戦闘など共通の画像表示サービスを作る
- 欠損時のフォールバックを確認する

### Step 4：Phase 5 キャラクターメイク統合

- 外見選択 UI と AppearanceSpec を接続する
- 再生成、候補比較、採用フローを実装する
- NPC 作成やスカウト対象生成にも再利用する

### Step 5：Runtime 生成（任意）

- ビルド別の有効 / 無効設定
- persistentDataPath への保存
- セーブデータとの整合性
- 外部接続同意、安全対策、容量管理
- 対応プラットフォームの制限表示

---

## 15. 初回実装時の完了条件

最初の ComfyUI 連携実装は、次を満たせば完了とする。

1. ComfyUI が停止していてもゲームと Unity Editor の通常作業に影響しない。
2. Editor 上で接続確認ができる。
3. バージョン付き workflow に prompt と seed を渡し、1 枚生成できる。
4. 生成中に Unity がフリーズしない。
5. 生成画像をプレビューし、採用または破棄できる。
6. 採用画像と GenerationRecord が対応付けて保存される。
7. キャラクターは visual ID を通じて画像を表示できる。
8. 画像欠損時は既定画像を表示できる。
9. Mock を用いた主要な EditMode テストが通る。
10. 必要なモデル、custom node、ライセンス、セットアップ手順が文書化される。

---

## 16. 未決定事項

実装前に次を決定する。

- 目標画風と画像仕様（バストアップ / 全身、解像度、背景透過など）
- 使用 checkpoint / LoRA とライセンス
- 最初の workflow と必要 custom node
- 生成画像を Git 管理するか
- CharacterDefinition とランタイムキャラクターのどちらへ visual ID を持たせるか
- Runtime 生成を製品機能として提供するか
- 対応プラットフォームと、ComfyUI を利用できない環境での表示

これらは Phase 1 のゲームサイクルを完成させた後、ComfyUI 連携 Step 1 の技術検証時に確定する。
