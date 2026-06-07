from django.http import JsonResponse
from django.shortcuts import redirect, render
from .models import Article
from .models import Sections
from .forms import  SectionFormSet, ArticleForm, ImageForm
import re
from django.core.paginator import Paginator
def main_page(request):
    articles = Article.objects.all().order_by('-views')[:12]

    return render(
        request,
        'main.html',
        {
            "articles": articles
        }
    )
def article_page(request, id):
    article = Article.objects.get(id=id)

    article.views += 1
    article.save(update_fields=["views"])

    sections = Sections.objects.filter(article=article)
    
    return render(request, 'article.html', {"article": article, "sections": sections})
def all_article_page(request):
    articles = Article.objects.all().order_by("id")

    paginator = Paginator(articles, 10)

    page_number = request.GET.get("page")
    page_obj = paginator.get_page(page_number)

    return render(
        request,
        'all_articles.html',
        {
            "page_obj": page_obj
        }
    )
def any_page(request):
    user_query = request.GET.get("query")
    if user_query == None:
        user_query = ""
    any_articles = Article.objects.filter(
        title__icontains=user_query
    )
    articles_result = []
   
    for any_article in any_articles:
        articles_result.append({'title' : any_article.title, 'id' : any_article.id})
    
    return JsonResponse(articles_result, safe=False)
def convert_links(text: str) -> str:
    pattern = r"\[\[([^|\]]+)\|([^\]]+)\]\]"

    def replacer(match):
        article_id = match.group(1)
        anchor = match.group(2)
        return f'<a href="/article/{article_id}">{anchor}</a>'

    return re.sub(pattern, replacer, text)

def new_article_page(request):

    if request.method == 'POST':

        form = ArticleForm(request.POST)
        image_form = ImageForm(request.POST, request.FILES)
        sections = SectionFormSet(request.POST)
        if form.is_valid() and sections.is_valid() and image_form.is_valid():
            image = image_form.save()
            article = form.save(commit=False)
            article.main_image = image
            article.description = convert_links(article.description)
            article.save()
            section_instances = sections.save(commit=False)
            
        for index, section in enumerate(section_instances):
            section.article = article
            section.order = index
            section.text = convert_links(section.text)
            section.save()
            return redirect('/')

    else:
        form = ArticleForm()
        image_form = ImageForm()
        sections = SectionFormSet()


    return render(request, "new_article.html", {
        "form": form,
        "image_form": image_form,
        "sections": sections
    })