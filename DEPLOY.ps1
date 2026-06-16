# ============================================================
# VetClinica — Deploy Railway (PowerShell)
# Executar de: C:\Projetos\vetclinica\
# ============================================================

# 1. Build do frontend
Set-Location frontend
npm run build

# 2. Prepara output Vercel
Set-Location ..
New-Item -ItemType Directory -Force -Path ".vercel\output\static"
Copy-Item -Recurse -Force "frontend\dist\*" ".vercel\output\static\"

# 3. Deploy Vercel
vercel deploy --prod --yes --prebuilt

# 4. Git commit e push (Railway faz o deploy automatico)
git add -A
git commit -m "multi-schema: isolamento por schema PostgreSQL"
git push origin main

Write-Host ""
Write-Host "Deploy concluido!" -ForegroundColor Green
Write-Host "Railway fara o rebuild automaticamente." -ForegroundColor Yellow
