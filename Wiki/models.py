from django.db import models

class Image(models.Model):
    source = models.ImageField()
    alt = models.TextField()

class Article(models.Model):
    title = models.CharField()
    main_image = models.ForeignKey(Image, on_delete=models.CASCADE)
    description = models.TextField()
    short_description = models.TextField(default=None, blank=True, null=True)
    views = models.PositiveIntegerField(default=0)

class Sections(models.Model):
    title = models.CharField()    
    order = models.IntegerField()
    text = models.TextField()
    article = models.ForeignKey(
        Article,
        on_delete=models.CASCADE,
        related_name='sections',
        null=True
    )

class Menu(models.Model):
    title = models.CharField()
    articles = models.ManyToManyField(Article)
class ArticleForm(models.Model):
    title = models.CharField()
    main_image = models.ForeignKey(Image, on_delete=models.CASCADE)
    description = models.TextField()
    short_description = models.TextField(default=None, blank=True, null=True)
