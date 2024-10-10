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

                const newTransform = $this.getAttribute('transform').replace(/translate\([^ ]+ (.+)\)/, `translate(${parseFloat(x) + parseFloat(215)} $1)`)
                console.log(newTransform)
                const newTrnsform = $this.getAttribute('transform').replace(/translate\([^ ]+ (.+)\)/, `translate(${parseFloat(x) + parseFloat(textLength - 20)} $1)`)
                console.log(newTrnsform)
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
            const margin = 10; // 10pxの隙間を持たせる
            const adjustedMaxWidth = maxWidth - margin;

            // テキストの現在の幅を取得
            let textWidth = textElement.getBBox().width;
            let fontSize = parseFloat(textElement.getAttribute('font-size'));

            // 最小フォントサイズを設定
            const minFontSize = 10;

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