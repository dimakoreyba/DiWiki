from django.http import JsonResponse
from django.shortcuts import redirect, render
from django.db.models import Case, IntegerField, Value, When
from .models import Article
from .models import Sections
from .forms import SectionFormSet, ArticleForm, ImageForm
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
    user_query = request.GET.get("query") or ""

    queryset = Article.objects.filter(title__icontains=user_query)

    exclude_id = request.GET.get("exclude_id")
    if exclude_id:
        try:
            queryset = queryset.exclude(id=int(exclude_id))
        except (ValueError, TypeError):
            pass

    queryset = queryset.annotate(
        exact_match=Case(
            When(title__iexact=user_query, then=Value(0)),
            default=Value(1),
            output_field=IntegerField(),
        )
    ).order_by("exact_match", "title")[:5]

    articles_result = [
        {"title": article.title, "id": article.id}
        for article in queryset
    ]

    return JsonResponse(articles_result, safe=False)


def convert_links(text: str) -> str:
    if not text:
        return text

    pattern = r"\[\[([^|\]]+)\|([^\]]+)\]\]"

    def replacer(match):
        article_id = match.group(1)
        anchor = match.group(2)
        return f'<a href="/article/{article_id}/">{anchor}</a>'

    return re.sub(pattern, replacer, text)


def save_article_with_sections(form, image_form, sections, is_edit=False):
    if is_edit:
        image_form.save()
        instance = form.save(commit=False)
    else:
        image = image_form.save()
        instance = form.save(commit=False)
        instance.main_image = image

    instance.description = convert_links(instance.description or "")
    if instance.short_description:
        instance.short_description = convert_links(instance.short_description)
    instance.save()

    section_instances = sections.save(commit=False)
    for index, section in enumerate(section_instances):
        section.article = instance
        section.order = index
        section.text = convert_links(section.text or "")
        section.save()

    return instance


def edit_article(request, id):
    article = Article.objects.get(id=id)

    if request.method == "POST":
        form = ArticleForm(request.POST, instance=article)
        image_form = ImageForm(
            request.POST,
            request.FILES,
            instance=article.main_image,
        )
        sections = SectionFormSet(request.POST, instance=article)

        if form.is_valid() and sections.is_valid() and image_form.is_valid():
            article = save_article_with_sections(
                form, image_form, sections, is_edit=True
            )
            return redirect(f"/article/{article.id}/")
    else:
        form = ArticleForm(instance=article)
        image_form = ImageForm(instance=article.main_image)
        sections = SectionFormSet(instance=article)

    return render(request, "new_article.html", {
        "form": form,
        "image_form": image_form,
        "sections": sections,
        "article_id": article.id,
        "is_edit": True,
    })


def new_article_page(request):
    if request.method == 'POST':
        form = ArticleForm(request.POST)
        image_form = ImageForm(request.POST, request.FILES)
        sections = SectionFormSet(request.POST)

        if form.is_valid() and sections.is_valid() and image_form.is_valid():
            save_article_with_sections(form, image_form, sections, is_edit=False)
            return redirect('/')
    else:
        form = ArticleForm()
        image_form = ImageForm()
        sections = SectionFormSet()

    return render(request, "new_article.html", {
        "form": form,
        "image_form": image_form,
        "sections": sections,
        "article_id": None,
        "is_edit": False,
    })
