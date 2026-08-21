# Phase 1 経営自動シミュレーション設計

## 1. 目的と範囲

Phase 1 では、画面上で戦闘を操作しなくても、ターンを進めるたびに CPU がギルドを運営し、資金・人員・状態が変化し続ける最小プロトタイプを作る。

本設計は [GuildFrontierSim 開発指示・基本設計](./GuildFrontierSimDevelopmentGuide.md) に従う。

対象：

- ギルドとキャラクターのランタイムデータ
- ターン進行
- CPU による防衛・遠征メンバー選択
- 自動戦闘
- 防衛戦と遠征の結果反映
- 資金、戦利品、給料、忠誠度
- 負傷、休業、入院、虜囚、帰還
- リーダー不在時の交代
- Debug.Log と最小 UI による状態確認
- EditMode テスト

対象外：

- プレイヤーによるメンバー手動選択
- コマンド RPG 戦闘
- セーブ・ロード
- キャラクターメイク、NPC、会話、スカウト
- ComfyUI および WPF キャラクター画像ツール
- 高度な CPU AI
- 完成版 UI と演出

---

## 2. 設計原則

- 純粋な C# クラスへゲームルールを置き、MonoBehaviour は起動と Unity UI 接続に限定する。
- ScriptableObject は初期値とバランス設定に使い、プレイ中の状態を書き込まない。
- CPU 選択、戦闘解決、結果反映を別々の責務にする。
- 乱数を抽象化し、テストでは結果を固定できるようにする。
- Phase 1 ではインターフェースを増やしすぎず、外部依存や差し替えが必要な境界だけ抽象化する。
- ID でキャラクターを参照し、ランタイムオブジェクトの重複や循環参照を避ける。
- 一つのターン処理を上から順に追えるようにし、暗黙の Unity イベント順序へ依存しない。

---

## 3. データモデル

### 3.1 マスターデータ

#### `CharacterDefinition`

ScriptableObject。キャラクターの初期値を保持する。

- `Id`
- `DisplayName`
- `StartingLevel`
- `MaxHp`
- `Attack`
- `Defense`
- `Speed`
- `Salary`
- `StartingLoyalty`
- 将来用の `VisualId`

`Id` は空文字と重複を許可しない。Phase 1 では Inspector で定義する。

#### `GuildStartingPreset`

ScriptableObject。Pattern B の開始状態を保持する。

- ギルド名
- 初期資金
- 初期メンバー定義一覧
- 初期リーダー ID

#### `BattleBalanceSettings`

ScriptableObject。調整可能な数値をまとめる。

- 攻撃、防御、HP、速度の戦闘力係数
- ランダム補正範囲
- 負傷・重傷・虜囚確率
- 防衛報酬範囲
- 遠征報酬範囲
- 給料支払い間隔
- 未払い時の忠誠度低下量
- 勝敗時の忠誠度変化量
- 回復に必要なターン数

#### `ExpeditionAreaDefinition`

ScriptableObject。Phase 1 では一つまたは少数の遠征先だけ用意する。

- `Id`
- 表示名
- 敵戦闘力
- 最大ステージ数
- 報酬倍率
- 救出対象が出現可能か

### 3.2 ランタイムデータ

#### `CharacterRuntimeData`

プレイ中に変化するキャラクター状態。ScriptableObject にしない。

- `CharacterId`
- `Level`
- `CurrentHp`
- `Loyalty`
- `Status`
- `UnavailableTurnsRemaining`
- `IsPlayerCharacter`

リーダーかどうかはキャラクター側の bool とギルド側 ID の二重管理にせず、`GuildRuntimeData.LeaderCharacterId` を正とする。

#### `CharacterStatus`

```text
Available
Expedition
Defending
Injured
Hospitalized
Captured
Resting
```

参加可能判定は状態値の比較を各所へ散らさず、`CharacterAvailability` など一か所へ集約する。

#### `GuildRuntimeData`

- `GuildName`
- `Funds`
- 所持アイテムと個数
- キャラクター一覧
- `LeaderCharacterId`
- `ActingLeaderCharacterId`
- 進行中の遠征一覧
- 現在ターン

Phase 1 のアイテムは文字列 ID と個数の小さなコレクションでよい。装備や効果は扱わない。

#### `ExpeditionRuntimeData`

- 遠征 ID
- 遠征先 ID
- 参加キャラクター ID 一覧
- 現在ステージ
- 一時獲得資金
- 一時獲得アイテム
- 救出済みキャラクター ID 一覧
- 遠征状態

戦利品は帰還完了まで `GuildRuntimeData` へ加算しない。

---

## 4. サービスと責務

### `GuildSimulation`

純粋な C# のアプリケーションサービス。Phase 1 のゲーム状態を所有し、1 ターンの処理を指揮する。

- 初期状態の生成
- `AdvanceTurn()` の公開
- 各サービスを決められた順序で呼び出す
- 発生したイベントを `SimulationLogEntry` として収集する

### `TurnProcessor`

一つのターンを順番に処理する。

1. 前ターンからの状態回復
2. リーダーと代理の有効性確認
3. 防衛イベント判定と処理
4. 進行中の遠征処理
5. 新規遠征の編成と開始
6. 給料支払い判定
7. 忠誠度と離脱判定
8. リーダー再確認
9. ターン番号更新

順番はテストで固定し、途中で参加不能になったキャラクターを後続処理が選ばないようにする。

### `CpuMemberSelector`

防衛・遠征メンバーをルールベースで選択する。

- `Available` のみを候補にする
- HP が基準未満のメンバーを除外する
- 戦闘力が高い順に優先する
- 防衛用の最低人数をギルドへ残す
- 同じターンに防衛と遠征へ重複参加させない

Phase 1 では同点時の順序を CharacterId で安定させる。

### `BattlePowerCalculator`

キャラクターまたはパーティーの戦闘力を計算する。計算式は `BattleBalanceSettings` の係数を使う。

### `BattleResolver`

味方戦闘力、敵戦闘力、乱数から共通の `BattleResult` を返す。資金加算や状態変更は行わない。

### `DefenseBattleProcessor`

- CPU から防衛メンバーを受け取る
- `BattleResolver` を呼ぶ
- 防衛報酬をギルドへ加算する
- 負傷、入院、休業を反映する
- ログを作成する

### `ExpeditionProcessor`

- 新規遠征の開始
- 進行中遠征の次ステージ処理
- 勝利時の一時戦利品追加
- 継続または帰還の CPU 判断
- 逃走成功時の減額と帰還
- 逃走失敗時の `Captured` 適用
- 帰還時の戦利品確定と状態復帰
- 虜囚救出

### `SalaryProcessor`

設定された間隔で給料を処理する。

- 全額支払い可能なら資金を減らす
- 不足時の扱いは Phase 1 では「支払えるメンバーだけ払う」ではなく、全員未払いとして統一する
- 未払い時は全対象メンバーの忠誠度を低下させる
- 結果をログへ記録する

### `RecoveryProcessor`

`Injured`、`Hospitalized`、`Resting` の残りターンを減らし、0 になったら `Available` へ戻す。`Captured` は時間経過では復帰させない。

### `LeadershipProcessor`

- リーダーが活動可能か確認する
- 一時不在なら代理ギルド長を選ぶ
- 長期継続不能または離脱なら新リーダーを決める
- 候補は活動可能な所属メンバーから選ぶ
- Phase 1 ではレベル、戦闘力、忠誠度、CharacterId の順で決定する

### `LoyaltyProcessor`

- 戦闘結果と給料結果による忠誠度変化
- `-100` 以下のキャラクターの離脱
- 遠征中、虜囚中など即時離脱させると矛盾する状態では、離脱予約として次の安全なタイミングまで遅延する

Phase 1 で離脱予約が過剰になる場合は、帰還・救出時に判定する最小実装でよい。

### `IRandomSource`

ゲーム乱数の境界。

- 実行時：`UnityRandomSource`
- テスト時：値を順番に返す `SequenceRandomSource`

### `SimulationLogEntry`

UI と Debug.Log の両方で使える構造化ログ。

- ターン番号
- カテゴリ
- メッセージ
- 関連キャラクター ID

ゲームルールから直接 `Debug.Log` を呼ばない。

---

## 5. データフロー

```text
GuildSimulation.AdvanceTurn()
          |
          v
     TurnProcessor
          |
          +--> RecoveryProcessor
          +--> LeadershipProcessor
          +--> CpuMemberSelector
          |       |
          |       +--> DefenseBattleProcessor --> BattleResolver
          |       +--> ExpeditionProcessor -----> BattleResolver
          +--> SalaryProcessor
          +--> LoyaltyProcessor
          |
          v
 GuildRuntimeData 更新 + SimulationLogEntry[]
          |
          v
 GuildSimulationController (MonoBehaviour)
          |
          +--> Debug.Log
          +--> Phase 1 最小 UI
```

ScriptableObject からランタイム状態を生成するのは開始時だけとし、その後は `GuildRuntimeData` を更新する。

---

## 6. 推奨フォルダ構成

```text
Assets/Game/
  Data/
    Definitions/
    Settings/
    Presets/
  Scripts/
    Domain/
      Characters/
      Guilds/
      Battles/
      Expeditions/
    Application/
      Simulation/
      Selection/
      Processing/
    Infrastructure/
      Random/
      Logging/
    Presentation/
      Simulation/
  Tests/
    EditMode/
  UI/
    Prefabs/
```

Phase 1 の規模ではフォルダと asmdef を細分化しすぎない。最初は Runtime 用 asmdef 一つと EditMode Tests 用 asmdef 一つを基本とする。

---

## 7. 最小 UI とシーン設定

`Assets/Scenes/MainScene.unity` を Phase 1 の確認シーンとして使用する。

Hierarchy の推奨構成：

```text
MainScene
  GuildSimulationRoot
    GuildSimulationController
  Canvas
    HeaderPanel
      TurnText
      FundsText
      LeaderText
    CharacterScrollView
      Viewport
        Content
    AdvanceTurnButton
    LogScrollView
      Viewport
        Content
  EventSystem
```

Canvas、Panel、Button、TextMeshPro、ScrollView、LayoutGroup は Unity Editor 上で配置する。スクリプトから UI GameObject を大量生成しない。

最初の一段階は `AdvanceTurnButton` と Debug.Log だけでもよい。ドメイン実装を UI 完成待ちにしない。

---

## 8. 実装順序

### Step 1：プロジェクト基盤

- `Assets/Game` フォルダ
- Runtime / EditMode Tests asmdef
- CharacterDefinition、設定 ScriptableObject の型
- `IRandomSource`

### Step 2：ランタイム状態

- CharacterStatus
- CharacterRuntimeData
- GuildRuntimeData
- 定義からランタイム状態を作る Factory
- 不変条件のテスト

### Step 3：戦闘の純粋ロジック

- BattlePowerCalculator
- BattleResult
- BattleResolver
- 固定乱数を使う EditMode テスト

### Step 4：CPU 選択

- CharacterAvailability
- CpuMemberSelector
- 重複参加と最低残留人数のテスト

### Step 5：防衛処理

- DefenseBattleProcessor
- 報酬、負傷、状態復帰のテスト

### Step 6：遠征処理

- ExpeditionRuntimeData
- ExpeditionProcessor
- 一時戦利品、帰還、逃走、虜囚、救出のテスト

### Step 7：経済と忠誠度

- SalaryProcessor
- LoyaltyProcessor
- 支払い成功、未払い、離脱のテスト

### Step 8：リーダーとターン統合

- RecoveryProcessor
- LeadershipProcessor
- TurnProcessor
- GuildSimulation
- 複数ターンの統合テスト

### Step 9：Unity 接続

- GuildSimulationController
- Pattern B の初期データアセット
- MainScene の最小 UI
- Debug.Log と画面表示による動作確認

各 Step はテストが通る状態で区切り、次の Step へ進む。

---

## 9. Phase 1 テスト観点

- 同じ定義から正しい初期状態が生成される
- 利用不能キャラクターが選択されない
- 防衛と遠征に同一人物が重複選択されない
- 戦闘結果が固定乱数で再現できる
- 防衛報酬が即時にギルド資金へ入る
- 遠征戦利品が帰還前にはギルド資産へ入らない
- 捕獲されたキャラクターが活動できない
- 救出・帰還後に正しい状態へ戻る
- 給料の支払いと未払いが正しく反映される
- 忠誠度 `-100` 以下で離脱する
- リーダー不在時に代理または新リーダーが選ばれる
- 複数ターン進めても状態の不変条件が壊れない
- 設定の最小値・最大値でも例外にならない

---

## 10. Phase 1 完了判定

次をすべて満たした時点で Phase 1 完了とする。

1. Pattern B の数人のメンバーと資金で開始できる。
2. ボタンまたはテストからターンを連続して進められる。
3. CPU が防衛・遠征メンバーを自動選択する。
4. 自動戦闘の結果が防衛・遠征へ反映される。
5. 資金、アイテム、給料、忠誠度が変化する。
6. 負傷、入院、休業、虜囚、救出、帰還が発生する。
7. リーダーが行動不能または離脱したとき、代理または新リーダーが決まる。
8. 状態変化を Debug.Log または最小 UI で確認できる。
9. 主要な EditMode テストが通る。
10. ComfyUI / WPF ツールがなくてもすべて動作する。
