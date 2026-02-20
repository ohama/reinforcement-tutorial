# 튜토리얼 사이트

프로젝트에 포함된 한국어 mdBook 튜토리얼을 빌드하고 배포하는 방법입니다.

## 로컬 빌드

```bash
mdbook build tutorial/
```

빌드 결과는 `docs/` 디렉토리에 출력됩니다 (`tutorial/book.toml`의 `build-dir = "../docs"` 설정).

빌드된 사이트를 브라우저에서 열려면:

```bash
open docs/index.html        # macOS
xdg-open docs/index.html    # Linux
start docs/index.html        # Windows
```

## 로컬 미리보기 (실시간 갱신)

```bash
mdbook serve tutorial/
```

브라우저에서 `http://localhost:3000`에 접속합니다. 소스 파일을 수정하면 자동으로 다시 빌드되고 브라우저가 새로고침됩니다.

## GitHub Pages 배포

이 프로젝트는 `docs/` 폴더를 GitHub Pages 소스로 사용합니다.

### 설정 방법

1. GitHub 저장소 → Settings → Pages
2. Source: **Deploy from a branch**
3. Branch: `main`, Folder: `/docs`
4. Save

### 배포 절차

```bash
# 1. 튜토리얼 빌드
mdbook build tutorial/

# 2. 빌드 결과 커밋
git add docs/
git commit -m "docs: update tutorial site"

# 3. 푸시하면 자동 배포
git push
```

## MathJax 수식

튜토리얼에서 LaTeX 수식을 사용합니다. `book.toml`에 `mathjax-support = true`가 설정되어 있습니다.

마크다운에서 수식 작성 시:

- 인라인 수식: `\\( Q(s,a) \\)`
- 블록 수식: `\\[ V(s) = \max_a Q(s,a) \\]`

mdBook의 MathJax는 `$...$` 구문을 지원하지 않으므로 반드시 `\\(...\\)` / `\\[...\\]` 구문을 사용해야 합니다.

## 설정 파일 참고

`tutorial/book.toml`:

```toml
[book]
title = "F#으로 배우는 강화학습"
language = "ko"

[build]
build-dir = "../docs"

[output.html]
mathjax-support = true
```
