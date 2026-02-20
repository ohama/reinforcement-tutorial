# 트러블슈팅

## .NET SDK 버전 오류

**증상:**

```
error NETSDK1045: The current .NET SDK does not support targeting .NET 10.0.
```

**해결:** .NET 10 SDK를 설치합니다. `dotnet --version`으로 10.x.x인지 확인하세요. 여러 버전이 설치된 경우 `dotnet --list-sdks`로 확인할 수 있습니다.

## TorchSharp 네이티브 라이브러리 로딩 실패

**증상 (Phase 4, 5):**

```
System.DllNotFoundException: Unable to load shared library 'LibTorchSharp'
```

**해결:**

1. 프로젝트를 클린 빌드합니다: `dotnet clean && dotnet build`
2. NuGet 캐시를 삭제합니다: `dotnet nuget locals all --clear`
3. 다시 빌드합니다: `dotnet build`

TorchSharp-cpu NuGet 패키지가 네이티브 라이브러리를 자동으로 다운로드합니다. 네트워크 문제로 다운로드가 실패한 경우 위 절차를 반복하세요.

## mdBook 설치 문제

**증상:**

```
zsh: command not found: mdbook
```

**해결:**

```bash
# macOS
brew install mdbook

# 또는 Cargo
cargo install mdbook
```

mdBook은 튜토리얼 사이트 빌드 전용입니다. Phase 코드 실행에는 필요 없습니다.

## FsCheck TypeLoadException

**증상:**

```
System.TypeLoadException: Could not load type 'FsCheck.StdGen'
```

**해결:** FsCheck 3.x가 설치된 경우 발생합니다. 이 프로젝트는 FsCheck **2.16.5**를 사용합니다. `.fsproj` 파일에서 FsCheck 버전이 `2.16.5`인지 확인하세요.

## 한글 깨짐

**증상:** 콘솔 출력에서 한글이 깨져 보임

**해결:**

- **Windows**: 터미널을 UTF-8로 설정합니다: `chcp 65001`
- **Windows Terminal** 사용을 권장합니다 (기본 UTF-8 지원)
- macOS/Linux에서는 일반적으로 문제가 발생하지 않습니다

## 긴 학습 시간

**Phase 4 (DQN):** 50K 에피소드 커리큘럼 학습에 수 분이 소요됩니다.

**Phase 5 (Gomoku MCTS):** 자가 대국 학습은 설정에 따라 수십 분 이상 소요될 수 있습니다.

학습이 너무 오래 걸리면:
- Phase 5의 경우 시뮬레이션 수를 줄여 실행할 수 있습니다 (메뉴에서 입력)
- 로그 파일(`logs/` 디렉토리)에서 학습 진행 상황을 확인할 수 있습니다

## 포트 충돌 (mdbook serve)

**증상:**

```
Error: Address already in use (os error 48)
```

**해결:** 다른 포트를 지정합니다:

```bash
mdbook serve tutorial/ -p 3001
```
