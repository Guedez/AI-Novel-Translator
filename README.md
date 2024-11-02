Run CrawlSyosetu to get however many chapters of your favorite novel from there and save them to disk
Create a .KNOWLEDGE file in the same folder you saved those raws, see the example on the project root
  This file is used to replace Japanese text with English text for consistent name/concept translation. Optional
Create a .PROMPT file in the same folder you saved those raws, see the example on the project root
  This is used to send the prompt for the AI to translate
Run BatchBooklet to translate as many chapters as you want

Currently it's hard coded to use "qwen2.5-14b-instruct" running on "localhost:1234", a future version will fix that
