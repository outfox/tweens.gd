import { defineCollection } from 'astro:content';
import { z } from 'astro/zod';
import { docsLoader, i18nLoader } from '@astrojs/starlight/loaders';
import { docsSchema, i18nSchema } from '@astrojs/starlight/schema';

export const collections = {
	// Starlight always queries UI translations. en.json keeps the collection nonempty
	// while inheriting all built-in English labels.
	i18n: defineCollection({ loader: i18nLoader(), schema: i18nSchema() }),
	docs: defineCollection({
		loader: docsLoader(),
		schema: docsSchema({
			extend: z.object({
				// Optional HTML for the hero; description stays plain text for page metadata.
				heroDescription: z.string().optional(),
				// Short facts under the landing hero, such as supported languages and engine versions.
				facts: z.array(z.object({ text: z.string(), link: z.string().optional() })).optional(),
			}),
		}),
	}),
};
