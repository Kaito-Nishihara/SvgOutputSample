// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
function updateTextPositions() {
    // idが"_Text"で終わる<text>タグをすべて取得
    const textElements = document.querySelectorAll('text[id$="_Text"]');

    textElements.forEach($this => {
        // text-anchorとtextLengthを取得
        const textAnchor = $this.getAttribute('text-anchor');
        const textLength = parseFloat($this.getAttribute('textLength')) || 0;

        // 現在のtransformのtranslate(x, y)を取得
        const transform = $this.getAttribute('transform');   
        const x = $this.getAttribute('transform').match(/translate\(([^ ]+) .+\)/)[1] ?? 0
        
        if (textLength != 0) {
            // 中央寄せ
            if (textAnchor === 'middle') {
                const newTransform = $this.getAttribute('transform').replace(/translate\([^ ]+ (.+)\)/, `translate(${parseFloat(x) + parseFloat(textLength / 2)} $1)`)
                $this.setAttribute('transform', newTransform)
            }

            // 右寄せ
            if (textAnchor === 'end') {

                const newTransform = $this.getAttribute('transform').replace(/translate\([^ ]+ (.+)\)/, `translate(${parseFloat(x) + parseFloat(textLength - 20)} $1)`)
                console.log(newTransform)
                $this.setAttribute('transform', newTransform)

            }
            // textLengthは不要なので表示時には削除（設定していた場合、textLengthの幅に合わせて文字間隔が大きくなってしまう）
            $this.removeAttribute('textLength');
        }
        else {
            //表示範囲が取れない場合はレイアウトが崩れるので強制左寄せとする
            $this.removeAttribute('text-anchor');
        }
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
        //実際の範囲より少し余裕を持たせる
        var maxWidth = rectBBox.width - 5;
        var maxHeight = rectBBox.height - 5;

        var fontSize = parseFloat($this.getAttribute("font-size") || "16");
        var { lines, fontSize: adjustedFontSize } = wrapTextToFit($this.textContent, maxWidth, maxHeight, fontSize);

        // 調整されたフォントサイズを設定
        $this.setAttribute("font-size", adjustedFontSize);

        // テキスト要素のクリアと新しい行の設定
        $this.innerHTML = '';

        // 行ごとに<tspan>を作成して追加する
        var y = parseFloat($this.getAttribute("y") || "0");
        for (var i = 0; i < lines.length; i++) {
            // 高さが表示範囲を超えたら処理を停止
            if ((y + i * adjustedFontSize) > maxHeight) break;

            var tspan = document.createElementNS("http://www.w3.org/2000/svg", "tspan");
            tspan.setAttribute("x", "0");
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
