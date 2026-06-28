from django import forms
from .models import *
from django.forms import inlineformset_factory
class ArticleForm(forms.ModelForm):
    class Meta:
        model=Article
        fields=['title', 'description', 'short_description']
SectionFormSet = inlineformset_factory(
    Article, Sections,
    fields=("title", "text"),
    extra=1,
    can_delete=False
)
EditSectionFormSet = inlineformset_factory(
    Article, Sections,
    fields=("title", "text"),
    extra=0,
    can_delete=False
)


class ImageForm(forms.ModelForm):
    class Meta:
        model = Image
        fields = ['source', 'alt']

    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        if self.instance.pk:
            self.fields['source'].required = False