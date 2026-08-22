# Phase 2 手動経営設計

## 1. 目的

Phase 2では、Phase 1で完成したCPU自動経営を維持しながら、ギルドの意思決定をプレイヤーが手動で行えるようにする。

対象となる判断は次のとおり。

- 防衛メンバー選択
- 遠征メンバー選択
- 遠征先選択
- 遠征継続・帰還選択
- 代理ギルド長選択

ゲームルールをUIへ移さず、CPUとプレイヤーが同じ検証済み入力モデルを使用する。プレイヤーがCPUへ任せることを明示的に選んだ場合は、Phase 1の自動判断をそのまま利用する。

---

## 2. Phase 2の範囲

### 含めるもの

- ギルドの操作主体と判断権限
- ターン開始前の経営計画
- 明示的なメンバー・遠征先選択
- 戦闘後の遠征継続判断
- 入力待ち状態と入力再検証
- CPUへ任せるフォールバック
- 手動経営用の最小UI
- CPU経営との互換性および統合テスト

### 含めないもの

- プレイヤーキャラクターの日常行動やクエスト
- コマンドRPG形式の戦闘操作
- 複数ギルドの同時管理
- リアルタイムの入力期限
- セーブ・ロード
- ComfyUIおよびWPF画像制作ツールの本格統合

これらはPhase 3以降または独立した画像制作計画で扱う。

---

## 3. 設計原則

### 3.1 判断と結果適用を分離する

Phase 1の一部処理は、CPU選択と結果適用を一つのProcessor内で行っている。Phase 2では次の形へ分離する。

```text
候補取得
   ↓
CPUまたはプレイヤーが選択
   ↓
共通Validator
   ↓
共通Processorが結果を適用
```

CPU専用ロジックを削除せず、CPUも共通入力を作る一つのDecision Providerとして扱う。

### 3.2 UIはIDを入力し、ルールを決めない

UIは選択可能候補を表示し、選ばれたCharacterIdやAreaIdを送信する。HP不足、状態異常、最低残留人数、重複参加などの判定はApplication層で行う。

### 3.3 入力待ち中にゲーム状態を部分更新しない

ターン開始前に必要な判断は、状態を変更する前に`TurnPlan`へ集める。未完成の計画では`TurnProcessor`を呼ばない。

戦闘結果を見て決める遠征継続判断だけは事前に確定できないため、遠征ステージ処理を「戦闘結果確定」と「継続判断適用」に分け、安全な中断点を作る。

---

## 4. 操作主体と権限

### `GuildControlMode`

```text
Cpu
Player
```

- `Cpu`: Phase 1と同様にすべて自動判断する。
- `Player`: プレイヤーに判断権がある項目を入力待ちにする。

### `GuildControlPolicy`

ランタイム設定として次を持つ。

- 操作モード
- プレイヤーキャラクターID
- 判断をCPUへ任せられるか
- 判断項目ごとの自動化設定

### 判断権の条件

Phase 2では、プレイヤーキャラクターが現在の正式リーダーまたは代理リーダーである場合に経営判断権を持つ。

プレイヤーに判断権がない場合はCPUが処理する。プレイヤーが遠征中、入院中、虜囚中などで代理リーダーでもない場合もCPUへ移行する。

---

## 5. ターン計画

### `TurnPlanningSession`

ターン開始前の入力状態を保持する純粋C#クラス。

- 対象ターン番号
- 計画作成時の状態Revision
- 必要な判断一覧
- 入力済み判断
- CPUへ任せる判断
- 完了状態

### `TurnDecisionType`

```text
DefenseMembers
ExpeditionMembers
ExpeditionArea
ActingLeader
```

遠征継続判断は戦闘後に発生するため、事前の`TurnPlanningSession`とは別の`PendingExpeditionDecision`として扱う。

### `TurnPlan`

検証済みのターン入力。

- 防衛イベントの有無
- 防衛参加者ID
- 新規遠征の有無
- 遠征参加者ID
- 遠征先ID
- 代理リーダーIDまたはCPU自動選択指定

`TurnPlan`は不変オブジェクトとし、完成後の変更を許可しない。

### 状態Revision

`GuildRuntimeData`に単調増加するRevisionを追加する。計画作成後に状態が変化した場合、古い計画を拒否して候補を再取得する。

Phase 2はシングルプレイヤーのターン制であるため、実時間による入力タイムアウトは設けない。「CPUに任せる」はプレイヤーの明示操作とする。

---

## 6. 共通選択入力

### 防衛

#### `DefenseAssignment`

- 敵戦闘力
- 防衛参加者ID一覧

#### 検証規則

- 参加者がギルド所属である
- IDが重複していない
- 全員が`Available`である
- HP条件を満たす
- 遠征参加者と重複しない
- 選択人数の上限を超えない

`DefenseBattleProcessor`へ明示参加者を受け取る経路を追加する。現在のCPU選択付き経路は互換用Adapterとして残す。

### 遠征開始

#### `ExpeditionAssignment`

- 遠征ID
- 遠征先ID
- 参加者ID一覧

#### 検証規則

- 遠征先が登録済みである
- 参加者がギルド所属である
- IDが重複していない
- 全員が`Available`である
- 防衛参加者と重複しない
- 最低残留人数を維持する
- 同じ遠征IDが存在しない

`ExpeditionProcessor`へ明示参加者を受け取る開始経路を追加し、CPUは`CpuMemberSelector`から同じ入力を生成する。

### 代理リーダー

#### `ActingLeaderAssignment`

- 候補者ID

候補者は活動可能、ギルド所属、離脱予定ではないことを必要とする。入力しない場合、明示的にCPU選択へ委譲できる。

---

## 7. 遠征継続判断

遠征ステージの勝利後、報酬と戦闘結果を見てから「継続」または「帰還」を選ぶ。

### `PendingExpeditionDecision`

- 遠征ID
- ステージ番号
- 戦闘結果
- 今回の報酬
- 一時戦利品合計
- 参加者の現在HP
- 選択肢: `Continue` / `Return` / `DelegateToCpu`
- 状態Revision

### 処理分割

```text
ExpeditionStageProcessor.ResolveStageBattle()
        ↓
勝利かつ最終ステージではない
        ↓
PendingExpeditionDecisionを作成して停止
        ↓
プレイヤーまたはCPUが判断
        ↓
ExpeditionStageProcessor.ApplyDecision()
```

敗北後の逃走・虜囚、最終ステージ後の帰還など、選択肢がない結果は入力待ちにしない。

入力待ちの遠征は再度戦闘処理されない。判断適用は一度だけ成功し、重複送信を拒否する。

---

## 8. シミュレーション状態機械

### `SimulationFlowState`

```text
Ready
PlanningTurn
WaitingForExpeditionDecision
ApplyingTurn
```

- `Ready`: 次のターンを開始できる。
- `PlanningTurn`: プレイヤーの事前入力を待っている。
- `WaitingForExpeditionDecision`: 遠征戦闘後の継続判断を待っている。
- `ApplyingTurn`: 状態更新中。UI入力を受け付けない。

不正な状態遷移は`InvalidOperationException`または失敗結果として拒否する。UIは状態に応じて利用可能なボタンだけを表示する。

### 基本フロー

```text
BeginTurnPlanning()
        ↓
必要なDecisionをUIへ提示
        ↓
SubmitDecision() / DelegateDecisionToCpu()
        ↓
TurnPlan完成
        ↓
ApplyTurnPlan()
        ↓
遠征継続判断が必要なら一時停止
        ↓
SubmitExpeditionDecision()
        ↓
ターン完了、Readyへ戻る
```

CPUモードでは同じフローを内部で即時に完了させ、Phase 1と同様に1回の操作でターンを進める。

---

## 9. 入力エラーとフォールバック

### 不正入力

次の場合は状態を変更せず、項目別エラーを返す。

- 存在しないキャラクターIDまたは遠征先ID
- 重複ID
- 選択後に状態が変わったキャラクター
- 防衛と遠征の重複参加
- 最低残留人数違反
- 入力待ちではない判断の送信
- 古いRevisionに対する入力
- 同じ判断の二重送信

### CPUフォールバック

- CPUモードでは常に自動判断する。
- Playerモードでは「CPUに任せる」を選んだ項目だけ自動判断する。
- プレイヤーに判断権がなくなった場合、未確定項目を破棄してCPU計画を再作成する。
- 候補者が存在しない場合はPhase 1と同じ安全な結果を返す。
- UIを閉じただけでは自動決定しない。

---

## 10. UI構成

Phase 1の状態表示を残し、次のパネルを追加する。

### ターン計画パネル

- 防衛イベント情報
- 防衛候補と選択済みメンバー
- 新規遠征の有無
- 遠征先一覧
- 遠征候補と選択済みメンバー
- 最低残留人数と警告
- 「計画を確定」
- 「すべてCPUに任せる」

### 遠征判断ダイアログ

- 戦闘結果
- 現在ステージ
- 参加者HP
- 一時戦利品
- 「さらに進む」
- 「帰還する」
- 「CPUに任せる」

### 代理リーダーパネル

- 現在の正式リーダー
- 代理候補一覧
- 候補のレベル、戦闘力、忠誠度、状態
- 「代理に指定」
- 「CPUに任せる」

Phase 2でもuGUIを使用する。候補表示は再利用可能な行Prefabへ分け、シーン上に全候補を固定配置しない。

---

## 11. ログ

既存の`SimulationLogEntry`へ次の内容を追加する。

- 判断者: Player / Cpu
- プレイヤーが選択した参加者
- CPUへ委譲した判断
- 入力が拒否された理由
- 遠征継続・帰還の判断
- 古い計画の破棄

ゲームルールから直接`Debug.Log`を呼ばず、Controllerが既存方式でUnity Consoleへ転送する。

---

## 12. 推奨クラス構成

```text
Application/Decisions/
  GuildControlMode
  GuildControlPolicy
  DecisionAuthority
  TurnDecisionType
  TurnPlanningSession
  TurnPlan
  TurnPlanBuilder
  DecisionValidationResult

Application/Assignments/
  DefenseAssignment
  DefenseAssignmentValidator
  ExpeditionAssignment
  ExpeditionAssignmentValidator
  ActingLeaderAssignment

Application/Processing/Expeditions/Decisions/
  PendingExpeditionDecision
  ExpeditionDecisionSubmission
  ExpeditionDecisionValidator

Application/Simulation/
  SimulationFlowState
  GuildSimulation

Presentation/
  TurnPlanningView
  ExpeditionDecisionView
  ActingLeaderSelectionView
```

インターフェースはCPUとプレイヤーで実際に差し替える境界に限定する。単一実装しかないProcessorまで抽象化しない。

---

## 13. 実装順序

### Step 10A: Phase 2設計

- 状態: 完了
- 本ドキュメント
- 判断権限、入力待ち、共通入力の確定

### Step 10B: 明示的な防衛割り当て

- 状態: 完了
- `DefenseAssignment`
- 共通Validator
- `DefenseBattleProcessor`の明示参加者経路
- CPU選択経路の互換テスト

### Step 10C: 明示的な遠征開始

- 状態: 完了
- `ExpeditionAssignment`
- 遠征先登録とValidator
- `ExpeditionProcessor`の明示参加者経路
- 防衛との重複参加テスト

### Step 10D: 操作主体とターン計画

- 状態: 完了
- `GuildControlMode`
- `GuildControlPolicy`
- `TurnPlanningSession`
- `TurnPlanBuilder`
- Revisionと古い入力の拒否

### Step 10E: 遠征継続・帰還判断

- 状態: 完了
- ステージ戦闘と判断適用の分離
- `PendingExpeditionDecision`
- 継続、帰還、CPU委譲、二重送信テスト

### Step 10F: 代理リーダー手動選択

- 状態: 完了
- 候補取得と明示指定
- プレイヤー権限喪失時のCPUフォールバック

### Step 10G: シミュレーション状態機械

- 状態: 完了
- Planning / Waiting / Applying / Ready
- CPUモードとの互換性
- ターン中断・再開の統合テスト

### Step 10H: 手動経営UI

- 状態: 進行中（ターン計画パネル完了、遠征判断ダイアログ未実装）
- ターン計画パネル
- 遠征判断ダイアログ
- 代理リーダー選択
- 入力エラー表示

### Step 10I: Phase 2完成確認

- CPU自動経営の回帰テスト
- 手動経営の複数ターンPlayModeテスト
- READMEと完成確認レポート更新

各StepはEditModeテストが通る状態で区切る。UI追加後はPlayModeテストも通す。

---

## 14. テスト観点

- CPUモードの結果がPhase 1から変わらない
- 明示選択したメンバーだけが防衛・遠征へ参加する
- 利用不能メンバーを選択できない
- 防衛と遠征へ同一人物を割り当てられない
- 最低残留人数を破れない
- 候補表示後に状態が変わった入力を拒否する
- 入力待ち中にターン番号や資金が変化しない
- CPU委譲した判断だけ自動決定される
- 遠征戦闘結果を確認して継続・帰還を選べる
- 遠征判断を二重適用できない
- プレイヤーに権限がない場合はCPUが継続する
- 正式リーダーまたは代理リーダーだけが経営判断できる
- 古いRevisionの計画を適用できない
- リセット後に入力待ち状態が残らない
- 既存の長期シミュレーションテストが通る

---

## 15. Phase 2完了条件

1. CPUモードでPhase 1と同じ自動経営を継続できる。
2. プレイヤーが防衛メンバーを選択できる。
3. プレイヤーが遠征メンバーと遠征先を選択できる。
4. プレイヤーが遠征継続・帰還を選択できる。
5. プレイヤーが代理リーダーを指定できる。
6. 不正入力でゲーム状態が部分更新されない。
7. 入力待ち状態をUIで確認できる。
8. プレイヤーが項目単位または一括でCPUへ判断を委譲できる。
9. 手動経営を複数ターン続けられる。
10. EditModeおよびPlayModeの主要テストが通る。

---

## 16. 最初の実装判断

最初にStep 10Bの防衛割り当てから着手する。防衛は一つのターン内で判断が完結し、遠征継続のような中断状態を必要としないため、CPU選択と共通Processorを分離する最小の検証対象として適している。

Step 10BではUIをまだ変更しない。EditModeテストから明示的なCharacterIdを入力し、Phase 1のCPU経路と同じ結果適用処理を通せる状態を完成条件とする。
