// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
function updateTextPositions() {
    // idが"_Text"で終わる<text>タグをすべて取得
    const textElements = document.querySelectorAll('text[id$="_Text"]');

    textElements.forEach($text => {
        // text-anchor と textLength を取得
        const textAnchor = $text.getAttribute('text-anchor');
        const textLength = parseFloat($text.getAttribute('textLength')) || 0;

        // <tspan> を取得 (複数行の場合に対応)
        const tspanElements = $text.querySelectorAll('tspan');

        tspanElements.forEach($tspan => {
            // 現在の x, y 座標を取得
            const x = parseFloat($tspan.getAttribute('x')) || 0;
            const y = parseFloat($tspan.getAttribute('y')) || 0;

            if (textLength !== 0) {
                let newX = x;

                // 中央寄せ
                if (textAnchor === 'middle') {
                    newX += textLength / 2;
                }

                // 右寄せ
                if (textAnchor === 'end') {
                    newX += textLength - 20; // 必要に応じて微調整
                }

                // <tspan> に新しい x を設定
                $tspan.setAttribute('x', newX);
            } else {
                // 表示範囲が取れない場合は強制的に左寄せ
                $text.removeAttribute('text-anchor');
            }
        });

        // textLength 属性は不要なので削除
        $text.removeAttribute('textLength');
    });
}


function resizeTextToFit() {
    // すべてのg要素(Area)を取得
    const areas = document.querySelectorAll('g[id$="Area"]');
    areas.forEach(area => {
        // Areaのidに基づいて対応するtext要素のidを生成 (_小計_ など)
        const areaId = area.getAttribute('id');
        const textElement = document.querySelector(`#${areaId.replace("_Area", "")}_Text`);

        if (textElement) {
            const rectElement = area.querySelector('rect'); // エリア内のrect要素
            const maxWidth = rectElement.getAttribute('width'); // エリアの幅
            if (maxWidth != null)
            {
                // 隙間（マージン）を設定
                const margin = 20; // 20pxの隙間を持たせる
                const adjustedMaxWidth = maxWidth - margin;

                // テキストの現在の幅を取得
                let textWidth = textElement.getBBox().width;
                let fontSize = parseFloat(textElement.getAttribute('font-size'));

                // 最小フォントサイズを設定
                const minFontSize = 7;

                // テキストの幅が親エリアの幅を超える限りフォントサイズを縮小
                while (textWidth > adjustedMaxWidth && fontSize > minFontSize) {
                    fontSize -= 0.5; // フォントサイズを0.5pxずつ減少
                    textElement.setAttribute('font-size', fontSize);

                    // 再計測
                    try {
                        textWidth = textElement.getBBox().width;
                    } catch (error) {
                        console.error("BBoxの計算中にエラーが発生しました。", error);
                        break;
                    }
                }
            }
          
        }
    });
}

function adjustTextArea() {
    var textElements = document.querySelectorAll('text[id$="_TextArea"]');
    textElements.forEach($this => {
        var id = $this.getAttribute("id");
        var bbox = $this.getBBox();
        var width = bbox.width;

        const areaElement = document.querySelector(`#${id.replace("_TextArea", "_Area")}`);
        var rectBBox = areaElement.getBBox(); // 表示範囲の幅と高さを取得
        // 実際の範囲より少し余裕を持たせる
        var maxWidth = rectBBox.width - 5;
        var maxHeight = rectBBox.height - 5;

        var fontSize = parseFloat($this.getAttribute("font-size") || "16");
        var tspanElement = $this.querySelector('tspan');

        // tspan から x と y の値を取得
        var x = parseFloat(tspanElement.getAttribute("x") || "0");
        var y = parseFloat(tspanElement.getAttribute("y") || "0");

        var { lines, fontSize: adjustedFontSize } = wrapTextToFit($this.textContent, maxWidth, maxHeight, fontSize);

        // 調整されたフォントサイズを設定
        $this.setAttribute("font-size", adjustedFontSize);

        // テキスト要素のクリアと新しい行の設定
        $this.innerHTML = '';

        // 行ごとに<tspan>を作成して追加する
        for (var i = 0; i < lines.length; i++) {
            // 高さが表示範囲を超えたら処理を停止
            if ((y + i * adjustedFontSize) > maxHeight) break;

            var tspan = document.createElementNS("http://www.w3.org/2000/svg", "tspan");
            tspan.setAttribute("x", x.toString());
            tspan.setAttribute("y", (y + i * adjustedFontSize).toString());
            tspan.textContent = lines[i];
            $this.appendChild(tspan);
        }
    });
}


function wrapTextToFit(textContent, maxWidth, maxHeight, initialFontSize) {
    let fontSize = initialFontSize;
    let charSize = fontSize;
    let maxCharsPerLine = Math.floor(maxWidth / charSize);
    let maxLines = Math.floor(maxHeight / charSize);
    let lines = wrapText(textContent, maxCharsPerLine);

    // フォントサイズを小さくしてテキストを調整
    while (lines.length > maxLines) {
        fontSize *= 0.95;
        charSize = fontSize;
        maxCharsPerLine = Math.floor(maxWidth / charSize);
        maxLines = Math.floor(maxHeight / charSize);
        lines = wrapText(textContent, maxCharsPerLine);
    }

    return { lines, fontSize }; // 調整後のテキストとフォントサイズを返す
}

function wrapText(textContent, maxCharsPerLine) {
    let lines = [];
    let currentLine = '';

    // テキストを1文字ずつ処理
    for (let i = 0; i < textContent.length; i++) {
        const char = textContent[i];

        // 現在の行が指定された最大文字数を超えた場合、新しい行を開始
        if (currentLine.length + 1 > maxCharsPerLine) {
            lines.push(currentLine);
            currentLine = '';
        }
        currentLine += char;
    }

    // 最後の行を追加
    if (currentLine) {
        lines.push(currentLine);
    }

    return lines;
}
