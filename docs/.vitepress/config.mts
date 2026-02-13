import { defineConfig } from 'vitepress'

export default defineConfig({
  title: "lip",
  description: "A general package installer",
  themeConfig: {
    nav: [
      { text: 'Guide', link: '/getting-started' },
      { text: 'Concepts', link: '/concepts' },
      { text: 'Commands', link: '/commands/install' },
      { text: 'Reference', link: '/reference/package-manifest' }
    ],

    sidebar: [
      {
        text: 'Guide',
        items: [
          { text: 'Getting Started', link: '/getting-started' },
          { text: 'Concepts', link: '/concepts' },
          { text: 'FAQ', link: '/faq' }
        ]
      },
      {
        text: 'How-to Guides',
        items: [
            { text: 'Creating Packages', link: '/guides/creating-packages' },
            { text: 'Migrating Packages', link: '/guides/migrating-packages' },
            { text: 'Installing Dependencies', link: '/guides/installing-dependencies' },
            { text: 'Managing Configuration', link: '/guides/managing-configuration' },
            { text: 'Internal Mechanism', link: '/guides/internal-mechanism' }
        ]
      },
      {
        text: 'Commands',
        items: [
          { text: 'cache clean', link: '/commands/cache-clean' },
          { text: 'config', link: '/commands/config' },
          { text: 'init', link: '/commands/init' },
          { text: 'install', link: '/commands/install' },
          { text: 'list', link: '/commands/list' },
          { text: 'migrate', link: '/commands/migrate' },
          { text: 'uninstall', link: '/commands/uninstall' },
          { text: 'update', link: '/commands/update' },
          { text: 'version', link: '/commands/version' },
          { text: 'view', link: '/commands/view' }
        ]
      },
      {
        text: 'Reference',
        items: [
          { text: 'Package Manifest', link: '/reference/package-manifest' },
          { text: 'Configuration', link: '/reference/configuration' },
          { text: 'lipd', link: '/reference/lipd' }
        ]
      }
    ],

    socialLinks: [
      { icon: 'github', link: 'https://github.com/futrime/lip' }
    ]
  }
})
