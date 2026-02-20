# 환경 설정

## .NET 10 SDK

이 프로젝트는 **.NET 10**을 사용합니다. (.NET 9 이하에서는 빌드되지 않습니다.)

### 설치

- **macOS**: `brew install dotnet`
- **Windows**: [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)에서 .NET 10 SDK 설치
- **Linux (Ubuntu/Debian)**: [공식 설치 가이드](https://learn.microsoft.com/dotnet/core/install/linux) 참고

### 설치 확인

```bash
dotnet --version
# 10.x.x 이상이어야 합니다
```

## mdBook (선택 사항)

튜토리얼 사이트를 빌드하려면 mdBook이 필요합니다. Phase 코드 실행에는 필요하지 않습니다.

```bash
# macOS
brew install mdbook

# Cargo (모든 플랫폼)
cargo install mdbook
```

```bash
mdbook --version
# 0.4.x 이상
```

## 에디터 권장

**VS Code + Ionide** 조합을 권장합니다.

1. [VS Code](https://code.visualstudio.com/) 설치
2. 확장 마켓플레이스에서 **Ionide for F#** 설치
3. 프로젝트 폴더를 열면 자동으로 F# 언어 서비스가 활성화됨

## Phase 4-5 추가 요구사항

Phase 4(DQN)와 Phase 5(Gomoku MCTS)는 **TorchSharp-cpu** 패키지를 사용합니다. NuGet이 빌드 시 자동으로 네이티브 라이브러리를 다운로드하므로 별도 설치가 필요 없습니다.

첫 빌드 시 네이티브 라이브러리 다운로드에 시간이 걸릴 수 있습니다 (약 200MB).
