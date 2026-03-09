#!/usr/bin/env node
/**
 * Homepage-only scraper for https://www.stb.com.mk/
 *
 * Run:
 *   node tools/scrape-homepage.js
 *
 * Output:
 *   scraped-data/homepage.json
 *   scraped-media/homepage/
 *
 * Scope:
 *   - homepage only
 *   - no Umbraco import
 *   - no inner-page scraping
 */

const fs = require("node:fs/promises");
const path = require("node:path");

const BASE_URL = "https://www.stb.com.mk/";
const ROOT_DIR = path.resolve(__dirname, "..");
const DATA_DIR = path.join(ROOT_DIR, "scraped-data");
const MEDIA_ROOT = path.join(ROOT_DIR, "scraped-media", "homepage");
const OUTPUT_JSON_PATH = path.join(DATA_DIR, "homepage.json");

const ICON_COLOR_MAP = {
  "icon-black": "#313a46",
  "icon-blue": "#39afd1",
  "icon-green": "#0acf97",
  "icon-orange": "#fd7e14",
  "icon-petrol": "#02a8b5",
  "icon-purple": "#6b5eae",
  "icon-red": "#fa5c7c",
  "icon-yellow": "#ffbc00"
};

const HTTP_HEADERS = {
  "user-agent": "STBWeb Homepage Scraper/1.0",
  accept: "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8"
};

const downloadCache = new Map();
const usedOutputPaths = new Set();

async function main() {
  await fs.mkdir(DATA_DIR, { recursive: true });
  await fs.mkdir(MEDIA_ROOT, { recursive: true });

  const homepageHtml = await fetchText(BASE_URL);

  const data = {
    heroSlides: parseHeroSlides(homepageHtml),
    featureIconLinks: parseFeatureIconLinks(homepageHtml),
    applyOnline: parseApplyOnline(homepageHtml),
    promoBanners: parsePromoBanners(homepageHtml),
    latestNews: parseLatestNews(homepageHtml)
  };

  for (let index = 0; index < data.heroSlides.length; index += 1) {
    const slide = data.heroSlides[index];
    slide.desktopImageLocalPath = await downloadMedia(
      slide.desktopImageUrl,
      "hero",
      `${String(index + 1).padStart(2, "0")}-desktop`
    );
    slide.mobileImageLocalPath = await downloadMedia(
      slide.mobileImageUrl,
      "hero",
      `${String(index + 1).padStart(2, "0")}-mobile`
    );
  }

  data.applyOnline.imageLocalPath = await downloadMedia(
    data.applyOnline.imageUrl,
    "apply"
  );

  for (const promoBanner of data.promoBanners) {
    promoBanner.imageLocalPath = await downloadMedia(
      promoBanner.imageUrl,
      "promo",
      sanitizeFilename(promoBanner.name)
    );
  }

  for (const newsItem of data.latestNews) {
    newsItem.thumbnailLocalPath = await downloadMedia(
      newsItem.thumbnailUrl,
      "news"
    );
  }

  await fs.writeFile(OUTPUT_JSON_PATH, `${JSON.stringify(data, null, 2)}\n`, "utf8");

  console.log(`Saved JSON to ${toRepoRelative(OUTPUT_JSON_PATH)}`);
  console.log(`Saved media under ${toRepoRelative(MEDIA_ROOT)}`);
}

function parseHeroSlides(html) {
  const section = extractSection(html, '<div id="homeMainSlider"', "<!-- End Coarousel-->");
  const blocks = sliceByStart(section, /<div class="carousel-item\b[^>]*>/g);

  return blocks.map((block) => ({
    slideHeading: readInnerText(block, /<h3>([\s\S]*?)<\/h3>/i),
    slideSubheading: readInnerText(block, /<h4>([\s\S]*?)<\/h4>/i),
    slideCtaText: readInnerText(block, /<a[^>]*class="[^"]*btn[^"]*"[^>]*>([\s\S]*?)<\/a>/i),
    slideCtaUrl: normalizeUrl(readAttribute(block, /<a[^>]*href="([^"]+)"/i)),
    desktopImageUrl: normalizeUrl(
      readAttribute(block, /<img[^>]*class="[^"]*d-none d-md-block[^"]*"[^>]*src="([^"]+)"/i) ||
        readAttribute(block, /<img[^>]*src="([^"]+)"/i)
    ),
    mobileImageUrl: normalizeUrl(
      readAttribute(block, /<img[^>]*class="[^"]*d-block d-md-none[^"]*"[^>]*src="([^"]+)"/i)
    ),
    desktopImageLocalPath: "",
    mobileImageLocalPath: ""
  }));
}

function parseFeatureIconLinks(html) {
  const section = extractSection(html, '<div class="home-what-i-need">', '<div class="container-fluid">\r\n    <div class="row pt-3 home-block-main">')
    || extractSection(html, '<div class="home-what-i-need">', '<div class="container-fluid">\n    <div class="row pt-3 home-block-main">');

  return [...section.matchAll(/<div class="win-icon-col">([\s\S]*?)<\/div>/g)].map((match) => {
    const block = match[1];
    const iconCircleClasses = readClassList(block, /<span class="([^"]*icon-circle[^"]*)"/i);
    const colorClass = iconCircleClasses.find((className) => className.startsWith("icon-") && className !== "icon-circle") || "";
    const iconClasses = readClassList(block, /<i class="([^"]+)"/i);
    const iconMdiClass = iconClasses.find((className) => className.startsWith("mdi-")) || "";

    return {
      label: readInnerText(block, /<span class="icon-text">([\s\S]*?)<\/span>/i),
      linkUrl: normalizeUrl(readAttribute(block, /<a[^>]*href="([^"]+)"/i)),
      iconMdiClass,
      iconColor: ICON_COLOR_MAP[colorClass] || ""
    };
  });
}

function parseApplyOnline(html) {
  const section = extractSection(html, '<div class="row pt-3 home-block-main">', '<div class="bg-primary"');
  const applyBlockMatch = section.match(
    /<div class="col-md-6 col-xl-4">([\s\S]*?<h4 class="header-title mt-0"[^>]*>\s*АПЛИЦИРАЈ ONLINE\s*<\/h4>[\s\S]*?)<\/div>\s*<!-- end card-->/i
  );
  const block = applyBlockMatch ? applyBlockMatch[1] : "";

  return {
    imageUrl: normalizeUrl(readAttribute(block, /<img[^>]*src="([^"]+)"/i)),
    imageLocalPath: "",
    targetUrl: normalizeUrl(readAttribute(block, /<div class="float-right">[\s\S]*?<a[^>]*href="([^"]+)"/i))
  };
}

function parsePromoBanners(html) {
  const section = extractSection(html, '<div class="container-fluid web-sites">', '</div> <!-- content -->');
  const blocks = sliceByStart(section, /<div class="col-sm-6 col-xl-5\b[^"]*">/g);

  return blocks
    .map((block) => ({
      name: readInnerText(block, /<h4[^>]*>([\s\S]*?)<\/h4>/i),
      targetUrl: normalizeUrl(readAttribute(block, /<a[^>]*href="([^"]+)"/i)),
      imageUrl: normalizeUrl(readAttribute(block, /<img[^>]*src="([^"]+)"/i)),
      imageLocalPath: ""
    }))
    .filter((promo) => promo.name || promo.targetUrl || promo.imageUrl);
}

function parseLatestNews(html) {
  const section = extractSection(html, '<div id="newsSlider"', '</div><!-- end newsSlider-->');
  const blocks = sliceByStart(section, /<div class="carousel-item\b[^>]*>/g);

  return blocks.map((block) => {
    const publishDate = readInnerText(block, /<span class="news-title-date">([\s\S]*?)<\/span>/i);
    const titleHtml = readRaw(block, /<p class="home-news-title mt-2">([\s\S]*?)<\/p>/i);
    const excerptHtml = readRaw(block, /<div class="col pl-0">([\s\S]*?)<\/div>/i);
    const excerptWithoutLink = excerptHtml.replace(/<a\b[^>]*>[\s\S]*?<\/a>/gi, " ");
    const titleWithoutDate = titleHtml.replace(/<span class="news-title-date">[\s\S]*?<\/span>/i, " ");
    const titleText = compactWhitespace(stripTags(titleWithoutDate));

    return {
      title: titleText,
      url: normalizeUrl(readAttribute(block, /<div class="col pl-0">[\s\S]*?<a[^>]*href="([^"]+)"/i)),
      publishDate,
      excerpt: compactWhitespace(stripTags(excerptWithoutLink)),
      thumbnailUrl: normalizeUrl(readAttribute(block, /<img[^>]*src="([^"]+)"/i)),
      thumbnailLocalPath: ""
    };
  });
}

async function downloadMedia(rawUrl, folderName, prefix = "") {
  const url = normalizeUrl(rawUrl);
  if (!url) {
    return "";
  }

  if (downloadCache.has(url)) {
    return downloadCache.get(url);
  }

  const folderPath = path.join(MEDIA_ROOT, folderName);
  await fs.mkdir(folderPath, { recursive: true });

  try {
    const response = await fetch(url, { headers: HTTP_HEADERS });
    if (!response.ok) {
      console.warn(`Skipping ${url}: ${response.status} ${response.statusText}`);
      return "";
    }

    const buffer = Buffer.from(await response.arrayBuffer());
    const filename = buildOutputFilename(url, response.headers.get("content-type"), prefix);
    const outputPath = ensureUniqueOutputPath(folderPath, filename);

    await fs.writeFile(outputPath, buffer);

    const relativePath = toRepoRelative(outputPath);
    downloadCache.set(url, relativePath);
    return relativePath;
  } catch (error) {
    console.warn(`Skipping ${url}: ${error.message}`);
    return "";
  }
}

function buildOutputFilename(url, contentType, prefix = "") {
  let filename = sanitizeFilename(path.basename(new URL(url).pathname));
  if (!path.extname(filename)) {
    filename += extensionFromContentType(contentType);
  }

  if (!filename || filename === ".bin") {
    filename = `file${extensionFromContentType(contentType)}`;
  }

  if (!prefix) {
    return filename;
  }

  return `${sanitizeFilename(prefix)}-${filename}`;
}

function ensureUniqueOutputPath(folderPath, filename) {
  const parsed = path.parse(filename);
  let candidatePath = path.join(folderPath, filename);
  let counter = 2;

  while (usedOutputPaths.has(candidatePath)) {
    candidatePath = path.join(folderPath, `${parsed.name}-${counter}${parsed.ext}`);
    counter += 1;
  }

  usedOutputPaths.add(candidatePath);
  return candidatePath;
}

function extractSection(html, startToken, endToken) {
  const startIndex = html.indexOf(startToken);
  if (startIndex === -1) {
    return "";
  }

  const endIndex = html.indexOf(endToken, startIndex);
  return endIndex === -1 ? html.slice(startIndex) : html.slice(startIndex, endIndex);
}

function sliceByStart(text, startRegex) {
  const starts = [...text.matchAll(startRegex)].map((match) => match.index);
  return starts.map((startIndex, index) => {
    const endIndex = starts[index + 1] ?? text.length;
    return text.slice(startIndex, endIndex);
  });
}

function readAttribute(text, regex) {
  const match = text.match(regex);
  return match ? decodeHtml(match[1]).trim() : "";
}

function readRaw(text, regex) {
  const match = text.match(regex);
  return match ? match[1] : "";
}

function readInnerText(text, regex) {
  const raw = readRaw(text, regex);
  return compactWhitespace(stripTags(raw));
}

function readClassList(text, regex) {
  const classValue = readAttribute(text, regex);
  return classValue.split(/\s+/).filter(Boolean);
}

function stripTags(text) {
  return decodeHtml(
    text
      .replace(/<script\b[\s\S]*?<\/script>/gi, " ")
      .replace(/<style\b[\s\S]*?<\/style>/gi, " ")
      .replace(/<br\s*\/?>/gi, " ")
      .replace(/<[^>]+>/g, " ")
  );
}

function decodeHtml(text) {
  return text
    .replace(/&#(\d+);/g, (_, codePoint) => String.fromCodePoint(Number(codePoint)))
    .replace(/&#x([0-9a-f]+);/gi, (_, codePoint) => String.fromCodePoint(parseInt(codePoint, 16)))
    .replace(/&nbsp;/gi, " ")
    .replace(/&amp;/gi, "&")
    .replace(/&quot;/gi, "\"")
    .replace(/&#39;/g, "'")
    .replace(/&lt;/gi, "<")
    .replace(/&gt;/gi, ">");
}

function compactWhitespace(text) {
  return text.replace(/\s+/g, " ").trim();
}

function sanitizeFilename(value) {
  return value
    .toLowerCase()
    .replace(/[^a-z0-9._-]+/g, "-")
    .replace(/-+/g, "-")
    .replace(/^-|-$/g, "") || "file";
}

function normalizeUrl(value) {
  if (!value) {
    return "";
  }

  try {
    return new URL(value, BASE_URL).toString();
  } catch {
    return "";
  }
}

function extensionFromContentType(contentType) {
  if (!contentType) {
    return ".bin";
  }

  if (contentType.includes("image/jpeg")) {
    return ".jpg";
  }
  if (contentType.includes("image/png")) {
    return ".png";
  }
  if (contentType.includes("image/webp")) {
    return ".webp";
  }
  if (contentType.includes("image/svg")) {
    return ".svg";
  }

  return ".bin";
}

function toRepoRelative(filePath) {
  return path.relative(ROOT_DIR, filePath).replace(/\\/g, "/");
}

async function fetchText(url) {
  const response = await fetch(url, { headers: HTTP_HEADERS });
  if (!response.ok) {
    throw new Error(`Failed to fetch ${url}: ${response.status} ${response.statusText}`);
  }

  return response.text();
}

main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
