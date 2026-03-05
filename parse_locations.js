const fs = require('fs');

function extractLocations(htmlFilePath, outputFile) {
    const content = fs.readFileSync(htmlFilePath, 'utf8');
    const matches = content.matchAll(/details:\s*(\{.*?\})/g);
    const locations = [];
    for (const match of matches) {
        try {
            const loc = JSON.parse(match[1]);
            locations.push(loc);
        } catch (e) {
            console.error('Error parsing JSON:', match[1], e);
        }
    }
    fs.writeFileSync(outputFile, JSON.stringify(locations, null, 2));
    console.log(`Extracted ${locations.length} locations to ${outputFile}`);
}

extractLocations('reference/stb-crawl/www.stb.com.mk/sb-lokacii/mreza-na-bankomati/index.html', 'wwwroot/data/atms.json');
extractLocations('reference/stb-crawl/www.stb.com.mk/sb-lokacii/mreza-na-filijali/index.html', 'wwwroot/data/branches.json');
