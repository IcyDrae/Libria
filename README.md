# 🔥 Project Concept
## “Libria” – A Self-Hosted Digital Library Manager

Written in **C# (.NET 8)** on **Ubuntu**, using **MySQL** as the database, packaged with **Docker**.

It manages:  
- PDF books
- EPUB books
- Images
- Audio files
- Any documents (docx, txt, etc.)

It’s a mix between “self-hosted Google Drive” + “Calibre Web” + “Jellyfin for documents”.

---

# How to install

1. ``` git clone https://github.com/IcyDrae/Libria.git ```
2. ``` cd Libria ```
3. ``` mkdir data ```
4. ``` docker compose -f docker/docker-compose.yml up --build -d ```

---

# 🧱 Architecture

### Backend (C# / .NET 8)
- ASP.NET Core Web API
- Entity Framework Core → MySQL provider
- Background worker for metadata extraction
- Local filesystem storage

### Frontend
- Razor Pages

### Database (MySQL)
- **File**
- **FileMetadata**
- **FileTag**
- **Tag**
- **Collection**

---

# 🔄 File & Metadata Workflow
1. User uploads a file via UI or API.
2. File is stored to disk `/data/library/...`
3. A background service scans it and extracts metadata depending on type:
   - PDF — use `iText7` or `PdfPig`
   - EPUB — use `epublib-core` (C# port exists)
   - Images — use `ImageSharp`
   - Audio — use `TagLibSharp`
4. Metadata inserted into MySQL.
5. UI shows searchable library grid.

---

# 🔍 Features That Make It Awesome-Selfhosted Worthy

### Core Features
- Upload + organize all file types
- Automatic metadata extraction
- Full-text search (title, author, tags, etc.)
- Reading/preview mode for PDFs + EPUBs
- Audio streaming for music/podcasts
- Tag system
- Smart collections (auto-generated lists)

### Advanced (will make your repo blow up)
- Optional encryption at rest (AES)
- WebDAV endpoint → sync with apps
- API for mobile app
- Thumbnail generation for all file types
- Role-based users (admin/shared users)
- Versioning for updated files
- “Recently added”, “most viewed”, etc.

---

# 📦 Deployment

- `docker-compose.yml` with:
  - app container
  - mysql container
  - optional traefik/nginx reverse proxy

---

# ⭐ Why This Will Actually Get Stars

Because:
There is **NO polished all-in-one C# document/media library manager**.

Self-hosters LOVE:  
- privacy
- indexing
- metadata
- clean UI
- Docker

Your project covers that whole space.  

Self-hosted communities on Reddit + GitHub will immediately show interest if you:  
- Have a clean README
- Include screenshots
- Offer Docker
- Have MySQL support
- Offer metadata extraction (unique selling point)
